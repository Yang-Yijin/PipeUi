window.globalModal = {
    registerEsc: function (dotnetRef) {
        // if (window.__globalModalEscRegistered) return;
        // 简化写法：加花括号
        if (window.__globalModalEscRegistered) {
            return;
        }
        window.__globalModalEscRegistered = true;

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                // try { dotnetRef.invokeMethodAsync("CloseFromEsc"); } catch { }
                // 简化写法：try/catch 展开写
                try {
                    dotnetRef.invokeMethodAsync("CloseFromEsc");
                } catch (err) {
                    // 忽略错误
                }
            }
        });
    }
};


// window.resetFileInputById = (id) => {
//     const el = document.getElementById(id);
//     if (el) el.value = "";
// };
// 简化写法：用 function 代替箭头函数，if 加花括号
window.resetFileInputById = function (id) {
    var el = document.getElementById(id);
    if (el) {
        el.value = ""; // 清空，保证下次一定触发 change
    }
};

// window.openFileDialogById = (id) => {
//     const el = document.getElementById(id);
//     if (el) el.click();
// };
// 简化写法：同上
window.openFileDialogById = function (id) {
    var el = document.getElementById(id);
    if (el) {
        el.click();
    }
};
