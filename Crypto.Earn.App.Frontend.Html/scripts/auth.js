async function redirectAuthentication(method) {
    const authTabInfo = document.querySelector('#auth-tab-info');
    const authControls = document.querySelector('#auth-controls');

    authControls.setAttribute('disabled', 'true');
    authTabInfo.style.visibility = 'collapse';

    const authData = await api.auth.getAuthRedirectUri(method);
    
    window.chrome.webview.postMessage({
        action: 'authenticate',
        method: 'redirect',
        data: authData
    });
    
    authControls.removeAttribute('disabled');
    authTabInfo.style.visibility = 'visible';
}