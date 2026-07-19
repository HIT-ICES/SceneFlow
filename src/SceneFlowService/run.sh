#!/bin/bash

# Project virtual-environment directory.
VENV_DIR=".venv"
# Uvicorn startup arguments.
APP_MODULE="main:app"
HOST="0.0.0.0"
PORT="8000"

function start() {
    # Stop the service first if it is already running.
    if [ -f scdv_service.pid ]; then
        PID=$(cat scdv_service.pid)
        if kill -0 $PID 2>/dev/null; then
            echo "[INFO] 检测到服务已在运行，终止服务..."
            kill $PID
            rm -f scdv_service.pid
            sleep 1
        fi
    fi
    echo "[INFO] 更新代码..."
    if ! git pull ; then
        echo "[ERROR] 代码更新失败，请检查网络或Git配置。"
        exit 1
    fi
    echo "[INFO] 同步依赖..."
    uv sync
    if ! uv sync ; then
        echo "[ERROR] 依赖同步失败，请检查 uv 配置。"
        exit 1
    fi
    if [ ! -d "$VENV_DIR" ]; then
        echo "[ERROR] 未找到虚拟环境目录 $VENV_DIR，请先创建虚拟环境。"
        exit 1
    fi
    source "$VENV_DIR/bin/activate"
    echo "[INFO] 启动服务..."
    unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY all_proxy ALL_PROXY
    nohup uv run --no-sync uvicorn $APP_MODULE --host $HOST --port $PORT >/dev/null 2>&1 &
    echo $! > scdv_service.pid
    echo "[INFO] 服务已启动，PID: $(cat scdv_service.pid)"
}

function stop() {
    if [ -f scdv_service.pid ]; then
        PID=$(cat scdv_service.pid)
        if kill -0 $PID 2>/dev/null; then
            kill $PID
            echo "[INFO] 已停止服务，PID: $PID"
        else
            echo "[WARNING] 未找到运行中的进程，或已退出。"
        fi
        rm -f scdv_service.pid
    else
        echo "[WARNING] 未找到 scdv_service.pid，尝试查找并终止进程..."
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
        echo "用法: $0 {start|stop}"
        exit 1
        ;;
esac
