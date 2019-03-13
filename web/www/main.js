var config = {
    debug: false,
    protocol: window.location.protocol + '//',
    host: window.location.hostname,
    port: 8081
};

// 
const STATUS_READY = 0;
const STATUS_MEASURE = 1;
const STATUS_ERROR = 2;

function updateUI(status) {
	console.log(status, status.status);
	console.log('status: ', status.status);
    var statusText = '';
    var statusLight = '';
    switch (status.status) {
        case STATUS_READY:
            statusText = 'Готов к сканированию';
            statusLight = '#0AFF18';
            break;
        case STATUS_MEASURE:
            statusText = 'Идет измерение';
            statusLight = '#0079FF';
            break;
        case STATUS_ERROR:
            statusText = 'Ошибка при измерении';
            statusLight = '#FF0000';
            break;
    }

    updateUIStatus(statusText, statusLight);

    document.getElementById('measure').innerHTML = Number(status.weight).toFixed(2) + ' кг'
        + ' / ' + Number(status.length).toFixed(2) + ' см'
        + ' / ' + Number(status.width).toFixed(2) + ' см'
        + ' / ' + Number(status.height).toFixed(2) + ' см';

    document.getElementById('barcode').value = status.barcode;
    document.getElementById('rank').value = status.rank;
    document.getElementById('comment').value = status.comment;
}

function updateUIStatus(text, color) {
    document.getElementById('status-text').innerHTML = text;
    document.getElementById('status-light').style.backgroundColor = color;
}


window.onload = function() {
    if (document.body.scrollHeight > document.body.clientHeight) {
        document.getElementById('logo').style.display = 'none';
    }
    if (document.body.scrollHeight > document.body.clientHeight) {
        document.getElementById('footer').style.display = 'none';
    }
    initSocket();
};

var socket;

function isObject(arg) {
    return typeof arg === 'object' && arg !== null;
}

function sendStart() {
    socket.send(JSON.stringify({command: 'start'}));
}

function sendInputs() {
    var status = {barcode: document.getElementById('barcode').value, rank: document.getElementById('rank').value, comment: document.getElementById('comment').value};
    socket.send(JSON.stringify({command: 'status', status: status}));
}

function initSocket() {
    try {
        onmessage = function(event) {
            var data = isObject(event.data) ? event.data : JSON.parse(event.data);
            if (data && data.command === 'status') {
                updateUI(data.status);
            }
        };
        onclose = function(event) {
            updateUIStatus('Ошибка соединения', '#FF0000');
        };
        socket = new ReconnectingWebSocket((config.protocol === 'https:' ? 'wss://' : 'ws://') + config.host + ':' + config.port, [],{onmessage: onmessage, onclose: onclose});
    } catch (e) {
        updateUIStatus('Ошибка соединения', '#FF0000');
		// debugger;
    }
}