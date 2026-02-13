import argparse
import base64
import io
from statistics import median

import dash
import pandas as pd
import plotly.graph_objects as go
from dash import Input, Output, State, callback, dash_table, dcc, html, no_update


SERIES = {
    "Voltage": "voltage",
    "MovingAverage Voltage": "movingaverageav",
    "Current X": "currentx",
    "Current Y": "currenty",
    "Current Z": "currentz",
    "Corrected X": "correctedx",
    "Corrected Y": "correctedy",
    "Corrected Z": "correctedz",
}
ALIASES = {"sampletime": "time", "filteredvoltage": "voltage", "currentav": "voltage"}
NUM = {
    "time",
    "voltage",
    "currentx",
    "currenty",
    "currentz",
    "correctedx",
    "correctedy",
    "correctedz",
    "movingaverageav",
}


def norm(s):
    return "".join((s or "").strip().lower().split())


def f(v, d):
    try:
        return float((v or "").strip())
    except Exception:
        return d


def cols(df):
    return [{"name": c, "id": c} for c in df.columns]


def empty(title):
    fig = go.Figure()
    fig.update_layout(template="plotly_white", height=420, title=title)
    return fig


def read_df(df_json):
    return pd.read_json(io.StringIO(df_json), orient="split")


def parse_upload(contents):
    raw = base64.b64decode(contents.split(",", 1)[1]).decode("utf-8", errors="replace")
    lines, i, header = raw.splitlines(), 0, {}

    while i < len(lines) and not lines[i].strip():
        i += 1

    if i < len(lines) and lines[i].strip().lower() == "beginheader":
        i += 1
        while i < len(lines) and lines[i].strip().lower() != "endheader":
            p = lines[i].split(",", 1)
            if len(p) == 2 and p[0].strip():
                header[p[0].strip()] = p[1].strip()
            i += 1
        i += 1

    while i < len(lines) and not lines[i].strip():
        i += 1
    if i >= len(lines):
        return header, pd.DataFrame()

    df = pd.read_csv(io.StringIO("\n".join(lines[i:])))
    df = df.rename(columns={c: norm(c) for c in df.columns})

    for src, dst in ALIASES.items():
        if src in df.columns and dst not in df.columns:
            df[dst] = df[src]

    for c in (set(df.columns) & NUM):
        df[c] = pd.to_numeric(df[c], errors="coerce")

    if {"time", "voltage"} <= set(df.columns):
        df = df[df["time"].notna() & df["voltage"].notna()]

    return header, df.reset_index(drop=True)


def chart(df, selected, x0, x1, y0, y1):
    if df.empty or not selected:
        return empty("No data")

    x = df["time"] if "time" in df.columns else pd.Series(range(len(df)))
    fig = go.Figure()

    for name in selected:
        c = SERIES.get(name)
        if c not in df.columns:
            continue

        d = pd.DataFrame({"x": x, "y": df[c]}).dropna()
        if x0 is not None:
            d = d[d["x"] >= x0]
        if x1 is not None:
            d = d[d["x"] <= x1]
        if y0 is not None:
            d = d[d["y"] >= y0]
        if y1 is not None:
            d = d[d["y"] <= y1]
        if len(d) >= 2:
            fig.add_trace(go.Scattergl(x=d["x"], y=d["y"], mode="lines", name=name))

    title = f"{selected[0]} vs Time" if len(selected) == 1 else "Multi-Series vs Time"
    y_title = (
        "Voltage (V)"
        if len(selected) == 1 and selected[0] in {"Voltage", "MovingAverage Voltage"}
        else "Value"
    )
    fig.update_layout(template="plotly_white", height=420, title=title)
    fig.update_xaxes(title="Time")
    fig.update_yaxes(title=y_title)
    return fig


def detect(df, target, th):
    if "voltage" not in df.columns:
        return pd.DataFrame(columns=["time", "voltage", "deviation"])

    out = df[(df["voltage"] - target).abs() > th][["time", "voltage"]].copy()
    if out.empty:
        return out.reindex(columns=["time", "voltage", "deviation"])

    out["deviation"] = (out["voltage"] - target).abs()
    return out.sort_values("deviation", ascending=False)


def sampling(df):
    if "time" not in df.columns or len(df) < 2:
        return "Not enough data.", pd.DataFrame(columns=["index", "time", "dt", "expected"])

    t = pd.to_numeric(df["time"], errors="coerce").dropna().tolist()
    if len(t) < 2:
        return "Not enough valid time values.", pd.DataFrame(columns=["index", "time", "dt", "expected"])

    dt = [t[i + 1] - t[i] for i in range(len(t) - 1)]
    m = median(dt)
    rows = [
        {"index": i, "time": round(t[i], 6), "dt": round(d, 6), "expected": round(m, 6)}
        for i, d in enumerate(dt)
        if m > 0 and (d > m * 2 or d < m * 0.5)
    ]
    msg = f"Median dt: {m:.6g}s | Intervals: {len(dt)} | Irregular: {len(rows)}"
    return msg, pd.DataFrame(rows, columns=["index", "time", "dt", "expected"])


app = dash.Dash(__name__, title="Voltage Monitor")
app.layout = html.Div(
    [
        html.H4("Voltage Monitor"),
        dcc.Upload(id="u", children=html.Button("Upload CSV"), accept=".csv"),
        html.Div(id="msg"),
        html.Div(id="stats"),
        dcc.Checklist(id="s", options=[], value=[], inline=True),
        dcc.Input(id="x0", type="number", placeholder="x-min"),
        dcc.Input(id="x1", type="number", placeholder="x-max"),
        dcc.Input(id="y0", type="number", placeholder="y-min"),
        dcc.Input(id="y1", type="number", placeholder="y-max"),
        dcc.Graph(id="g"),
        dcc.Input(id="th", type="number", value=10, step=1),
        html.Button("Detect", id="b1", n_clicks=0),
        html.Div(id="am"),
        dash_table.DataTable(id="at", page_size=8),
        html.Button("Analyze Sampling", id="b2", n_clicks=0),
        html.Div(id="sm"),
        dash_table.DataTable(id="st", page_size=8),
        dcc.Store(id="df"),
        dcc.Store(id="target", data=120.0),
    ]
)


@callback(
    Output("msg", "children"),
    Output("stats", "children"),
    Output("s", "options"),
    Output("s", "value"),
    Output("df", "data"),
    Output("target", "data"),
    Input("u", "contents"),
    State("u", "filename"),
    prevent_initial_call=True,
)
def on_upload(contents, fn):
    if not contents:
        return (no_update,) * 6

    try:
        header, df = parse_upload(contents)
    except Exception as e:
        return f"Parse failed: {e}", "", [], [], None, 120.0

    if df.empty:
        return "Loaded 0 valid points.", "", [], [], None, 120.0

    choices = [k for k, v in SERIES.items() if v in df.columns]
    sel = ["Voltage"] if "Voltage" in choices else choices[:1]
    v = df["voltage"].dropna() if "voltage" in df.columns else pd.Series(dtype=float)
    stats = (
        f"Points: {len(df)}"
        if v.empty
        else f"Points: {len(df)} | Min: {v.min():.3f}V | Max: {v.max():.3f}V | Avg: {v.mean():.3f}V"
    )
    return (
        f"Loaded {len(df)} points from {fn or 'file'}",
        stats,
        [{"label": c, "value": c} for c in choices],
        sel,
        df.to_json(orient="split"),
        f(header.get("TargetVoltage", ""), 120.0),
    )


@callback(
    Output("g", "figure"),
    Input("df", "data"),
    Input("s", "value"),
    Input("x0", "value"),
    Input("x1", "value"),
    Input("y0", "value"),
    Input("y1", "value"),
)
def on_chart(df_json, s, x0, x1, y0, y1):
    if not df_json:
        return empty("Upload a CSV to start")
    return chart(read_df(df_json), s or [], x0, x1, y0, y1)


@callback(
    Output("am", "children"),
    Output("at", "data"),
    Output("at", "columns"),
    Input("b1", "n_clicks"),
    State("df", "data"),
    State("target", "data"),
    State("th", "value"),
    prevent_initial_call=True,
)
def on_detect(_, df_json, target, th):
    if not df_json:
        return "No data.", [], []

    out = detect(read_df(df_json), float(target or 120), float(th or 10))
    if out.empty:
        return f"No anomalies (threshold={th}V, target={target}V)", [], []

    show = out.head(200).round(6)
    return f"Found {len(out)} anomalies (showing {len(show)})", show.to_dict("records"), cols(show)


@callback(
    Output("sm", "children"),
    Output("st", "data"),
    Output("st", "columns"),
    Input("b2", "n_clicks"),
    State("df", "data"),
    prevent_initial_call=True,
)
def on_sampling(_, df_json):
    if not df_json:
        return "No data.", [], []

    msg, out = sampling(read_df(df_json))
    show = out.head(200)
    return msg, show.to_dict("records"), cols(show)


if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--host", default="127.0.0.1")
    p.add_argument("--port", type=int, default=5050)
    a = p.parse_args()
    app.run(debug=False, host=a.host, port=a.port)
