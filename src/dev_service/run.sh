#!/bin/bash


VENV_DIR=".venv"

APP_MODULE="main:app"
HOST="0.0.0.0"
PORT="8000"

function start() {

    if [ -f scdv_service.pid ]; then
        PID=$(cat scdv_service.pid)
        if kill -0 $PID 2>/dev/null; then
            echo "[INFO] Detected service already running, stopping service..."
            kill $PID
            rm -f scdv_service.pid
            sleep 1
        fi
    fi
    echo "[INFO] Updating code..."
    if ! git pull ; then
        echo "[ERROR] Code update failed, please check network or Git configuration."
        exit 1
    fi
    echo "[INFO] Synchronizing dependencies..."
    uv sync
    if ! uv sync ; then
        echo "[ERROR] Dependency synchronization failed, please check uv configuration."
        exit 1
    fi
    if [ ! -d "$VENV_DIR" ]; then
        echo "[ERROR] Virtual environment directory $VENV_DIR not found. Please create the virtual environment first."
        exit 1
    fi
    source "$VENV_DIR/bin/activate"
    echo "[INFO] Starting service..."
    unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY all_proxy ALL_PROXY
    nohup uvicorn $APP_MODULE --host $HOST --port $PORT >/dev/null 2>&1 &
    echo $! > scdv_service.pid
    echo "[INFO] Service started, PID: $(cat scdv_service.pid)"
}

function stop() {
    if [ -f scdv_service.pid ]; then
        PID=$(cat scdv_service.pid)
        if kill -0 $PID 2>/dev/null; then
            kill $PID
            echo "[INFO] Service stopped, PID: $PID"
        else
            echo "[WARNING] No running process found, or it has already exited."
        fi
        rm -f scdv_service.pid
    else
        echo "[WARNING] scdv_service.pid not found, attempting to find and terminate the process..."
        pkill -f "uvicorn $APP_MODULE"
    fi
}

case "$1" in
    start)
        start
        ;;
    stop)
        stop
        ;;
    *)
        echo "Usage: $0 {start|stop}"
        exit 1
        ;;
esac