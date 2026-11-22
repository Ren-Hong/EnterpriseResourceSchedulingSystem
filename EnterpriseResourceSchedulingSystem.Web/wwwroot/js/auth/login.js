const toggle = document.getElementById("toggle-password");
const password = document.getElementById("Password");

toggle.hidden = false;
toggle.addEventListener("click", () => {
    const showPassword = password.type === "password";
    password.type = showPassword ? "text" : "password";
    toggle.textContent = showPassword ? "隱藏" : "顯示";
    toggle.setAttribute("aria-label", showPassword ? "隱藏密碼" : "顯示密碼");
});
