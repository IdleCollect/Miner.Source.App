const defaultScreen = 'home';
const containers = [];
let all = [];

let hashPowerGraph, hashPowerGraphCtx;
let hashGraphData = []; let hashGraphLabels = [];

document.addEventListener('DOMContentLoaded', function() {
    // Screens:
    all = document.querySelectorAll('*[data-screen]');
    all.forEach(element => {
        if(element.tagName !== 'A' && element.tagName !== 'a')
            containers.push({el: element, defaultDisplay: element.style.display});
    });
    
    ShowScreen(defaultScreen);
    HookScreens();
    DrawGraph();
});

function HookScreens() {
    const navigators = document.querySelectorAll('nav a');
    navigators.forEach(navigator => {
        navigator.addEventListener('click', () => {
            ShowScreen(navigator.getAttribute('data-screen'));
        });
    });
}

function ShowScreen(screenName) {
    // Hide all except the selected one.
    containers.forEach(container => {
        const attributeValue = container.el.getAttribute('data-screen').split('|');
        if(attributeValue.includes(screenName))
            container.el.style.display = container.defaultDisplay;
        else
            container.el.style.display = 'none';
    });
    
    // Change icon coloring.
    const icons = document.querySelectorAll('a[data-screen]');
    icons.forEach(icon => {
        const attributeValue = icon.getAttribute('data-screen').split('|');
        if(attributeValue.includes(screenName))
            icon.children[0].src = icon.children[0].src.replace('default', 'active');
        else
            icon.children[0].src = icon.children[0].src.replace('active', 'default');
    });
}

window.chrome.webview.addEventListener('message', function(e) {
    switch(e.data.action) {
        case 'SyncAccount':
            UpdateVariable('name', e.data.user.Username);
            document.querySelector('#auto-startup-setting').checked = e.data.settings.autoStartup;
            document.querySelector('#disable-sleep-setting').checked = e.data.settings.disableSleep;
            break;
        case 'SyncMining':
            UpdateVariable('mining-speed', e.data.speed);
            UpdateVariable('mining-state', e.data.state);
            
            document.querySelector('#pause-button-resume-divider').style.display = e.data.isActive ? 'none' : 'list-item';
            document.querySelector('#pause-button-resume-button').style.display = e.data.isActive ? 'none' : 'list-item';
            
            if(e.data.speed) {
                let numbers = e.data.speed.split(' ')[0];
                let parsed = parseFloat(numbers);
                hashGraphData.shift();
                hashGraphData.push(parsed);
                hashPowerGraph.update();
            }
            break;
        case 'SyncBalance':
            UpdateVariable('balance-estimated-month', e.data.estimatedPerMonth);
            UpdateVariable('balance-total', e.data.currentBalance);
            UpdateVariable('balance-month', e.data.balanceMonth);
            UpdateVariable('miners-now', e.data.external.miners);
            break;
    }
});

function UpdateVariable(variableName, variableValue) {
    const toUpdate = document.querySelectorAll(`*[data-message*="{{${variableName}}}"]`);
    toUpdate.forEach(element => {
        const text = element.getAttribute('data-message');
        element.innerText = text.replace(`{{${variableName}}}`, variableValue); 
    });
}

/* */
function DrawGraph() {
    let width = 400, height = 150, gradient;
    function getGradient(ctx) {
            gradient = ctx.createLinearGradient(0, 0, 0, 150);
            gradient.addColorStop(0, 'rgba(0,105,255,0.7)');
            gradient.addColorStop(0.5, 'rgba(0,105,255,0.5)');
            gradient.addColorStop(1, 'rgba(0,105,255,0.15)');

        return gradient;
    }
    
    for(let i = 0; i < 10; i++) {
        hashGraphData.push( i > 5 ? 0 : 10);
        hashGraphLabels.push('value_' + i);
    }
    
    const hashPowerGraphCtx = document.getElementById('hash-power-graph').getContext('2d');
    hashPowerGraph = new Chart(hashPowerGraphCtx, {
        type: 'line',
        data: {
            labels: hashGraphLabels,
            datasets: [{
                data: hashGraphData,
                backgroundColor: function(context) {
                    return getGradient(hashPowerGraphCtx)
                },
                borderColor: [
                    '#0069FF'
                ],
                borderWidth: 2,
                cubicInterpolationMode: 'monotone',
                tension: 1,
                fill: true,
            }]
        },
        options: {
            plugins: {
                title: {
                    display: false
                },
                subtitle: {
                    display: false
                },
                legend: {
                    display: false
                },
                tooltip: {
                    enabled: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    display: false,
                    suggestedMax: 60
                },
                x: {
                    display: false
                }
            },
            layout: {
              autoPadding: false,
              padding: 10
            },
            responsive: true,
            maintainAspectRatio: false,
        }
    });
}

/* */
function Logout() {
    window.chrome.webview.postMessage({
        action: 'logout'
    });
}

function Quit() {
    window.chrome.webview.postMessage({
        action: 'quit'
    });
}

function PauseMining(duration) {
    window.chrome.webview.postMessage({
        action: 'mining-pause',
        duration: duration
    });
}

function AutoStartupToggle() {
    window.chrome.webview.postMessage({
        action: 'auto-startup-setting',
        value: document.querySelector('#auto-startup-setting').checked
    });
}
function NoSleepToggle() {
    window.chrome.webview.postMessage({
        action: 'no-sleep-setting',
        value: document.querySelector('#disable-sleep-setting').checked
    });
}