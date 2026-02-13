import sys
import io
import base64

import dash
from dash import dcc, html, dash_table
from dash import Input, Output, State, callback, no_update
import plotly.graph_objects as go
import pandas as pd
from statistics import median

app = dash.Dash(__name__, title="Voltage Monitor")

_data = {"df": None, "target": 120.0}


def parse_csv(text):
    lines = text.splitlines()
    header = {}
    i = 0

    if i < len(lines) and lines[i].strip().lower() == "beginheader":
        i += 1
        while i < len(lines) and lines[i].strip().lower() != "endheader":
            line = lines[i].strip()
            if line and "," in line:
                k, v = line.split(",", 1)
                header[k.strip()] = v.strip()
            i += 1
        if i < len(lines):
            i += 1

    while i < len(lines) and not lines[i].strip():
        i += 1

    data_text = "\n".join(lines[i:]).strip()
    if not data_text:
        return None, header

    return pd.read_csv(io.StringIO(data_text)), header


app.layout = html.Div([
    html.H3("Voltage Monitor"),
    dcc.Upload(id="upload", children=html.A("Upload CSV"), accept=".csv"),
    html.Div(id="msg"),
    dcc.Checklist(id="series", options=[], value=[], inline=True),
    html.Div(id="stats"),
    dcc.Graph(id="chart", style={"display": "none"}),

    html.Hr(),
    html.B("Anomaly Detection - Threshold: "),
    dcc.Input(id="thresh", type="number", value=10, step=1),
    html.Button("Detect", id="detect-btn", n_clicks=0),
    html.Div(id="anomaly-out"),

    html.Hr(),
    html.Button("Analyze Sampling", id="sampling-btn", n_clicks=0),
    html.Div(id="sampling-out"),
])


@callback(
    Output("msg", "children"),
    Output("series", "options"),
    Output("series", "value"),
    Output("stats", "children"),
    Output("chart", "figure"),
    Output("chart", "style"),
    Input("upload", "contents"),
    State("upload", "filename"),
    prevent_initial_call=True,
)
def on_upload(contents, fname):
    if not contents:
        return (no_update,) * 6

    df, hdr = parse_csv(base64.b64decode(contents.split(",")[1]).decode("utf-8"))

    if df is None or df.empty:
        return "Parse failed.", [], [], "", go.Figure(), {"display": "none"}

    _data["df"] = df
    _data["target"] = float(hdr.get("TargetVoltage", 120))

    opts = [c for c in df.columns if c != "Time"]
    val = ["Voltage"] if "Voltage" in df.columns else opts[:1]

    stats = f"Points: {len(df)}"
    if "Voltage" in df.columns:
        v = df["Voltage"].dropna()
        stats = f"Points: {len(df)} | Min: {v.min():.2f}V | Max: {v.max():.2f}V | Avg: {v.mean():.2f}V"

    return f"Loaded {len(df)} points from {fname}", opts, val, stats, build_chart(df, val), {}


@callback(
    Output("chart", "figure", allow_duplicate=True),
    Input("series", "value"),
    prevent_initial_call=True,
)
def on_series(sel):
    df = _data["df"]
    if df is None or not sel:
        return go.Figure()
    return build_chart(df, sel)


@callback(
    Output("anomaly-out", "children"),
    Input("detect-btn", "n_clicks"),
    State("thresh", "value"),
    prevent_initial_call=True,
)
def on_detect(_, thresh):
    df = _data["df"]
    if df is None or "Voltage" not in df.columns:
        return "No data."

    target = _data["target"]
    th = float(thresh or 10)
    bad = df[abs(df["Voltage"] - target) > th]

    if bad.empty:
        return f"No anomalies (threshold={th}V, target={target}V)"

    d = bad.head(50)[["Time", "Voltage"]].copy()
    d["Deviation"] = abs(d["Voltage"] - target).round(2)

    return html.Div([
        html.P(f"Found {len(bad)} anomalies"),
        dash_table.DataTable(data=d.round(4).to_dict("records"),
                             columns=[{"name": c, "id": c} for c in d.columns]),
    ])


@callback(
    Output("sampling-out", "children"),
    Input("sampling-btn", "n_clicks"),
    prevent_initial_call=True,
)
def on_sampling(_):
    df = _data["df"]
    if df is None or "Time" not in df.columns or len(df) < 2:
        return "Not enough data."

    times = df["Time"].dropna().values
    deltas = [times[i + 1] - times[i] for i in range(len(times) - 1)]
    med = median(deltas)

    bad = [{"Idx": i, "Time": round(times[i], 6), "Dt": round(d, 6), "Expected": round(med, 6)}
           for i, d in enumerate(deltas) if med > 0 and (d > med * 2 or d < med * 0.5)]

    result = f"Median dt: {med:.6g}s | Intervals: {len(deltas)} | Irregular: {len(bad)}"
    if not bad:
        return result

    return html.Div([
        html.P(result),
        dash_table.DataTable(data=bad[:50],
                             columns=[{"name": c, "id": c} for c in ["Idx", "Time", "Dt", "Expected"]]),
    ])


def build_chart(df, series_list):
    fig = go.Figure()
    x = df["Time"] if "Time" in df.columns else df.index
    for name in series_list:
        if name in df.columns:
            fig.add_trace(go.Scattergl(x=x, y=df[name], mode="lines", name=name))
    fig.update_layout(title=f"{', '.join(series_list[:3])} vs Time", template="plotly_white", height=400)
    return fig


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 5050
    app.run(debug=False, host="127.0.0.1", port=port)
