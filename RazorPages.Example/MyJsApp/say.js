const say = (message) => {
    const sayElement = document.createElement("div");
    sayElement.className = "say";
    sayElement.innerText = message;
    document.getElementById("app").append(message, sayElement);
};

export { say }