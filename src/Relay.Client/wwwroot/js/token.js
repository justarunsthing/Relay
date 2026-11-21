window.relayToken = {
    get: () => localStorage.getItem("relay.token"),
    set: (token) => localStorage.setItem("relay.token", token),
    remove: () => localStorage.removeItem("relay.token")
};
