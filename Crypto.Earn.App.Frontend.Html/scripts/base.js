const api = {
    uri: {
        api: (endpoint) => `https://api.idlecollect.com/${endpoint}`,
        dashboard: (endpoint) => `https://idlecollect.com/dashboard/${(endpoint ? endpoint : '')}`
    },
    client: {
        get: async (endpoint) => {
            return fetch(api.uri.api(endpoint), {
                method: 'GET',
                mode: 'cors',
                cache: 'no-cache',
                credentials: 'include',
            });
        },
        post: async (endpoint, body) => {
            try {
                return await fetch(api.uri.api(endpoint), {
                    method: 'POST',
                    mode: 'cors',
                    cache: 'no-cache',
                    credentials: 'include',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(body)
                });
            } catch (e) {
                return {
                    status: 500
                }
            }
        },
    },
    auth: {
        getAuthRedirectUri: async function (method) {
            const result = await api.client.get('auth/app/being?method=' + method);
            return await result.json();
        }
    }
};

const backend = {
    controls: {
        links: {
            navigate: (method, url) => {
                window.chrome.webview.postMessage({
                    action: 'redirect',
                    method: method,
                    url: url
                });
            }
        }
    }
}