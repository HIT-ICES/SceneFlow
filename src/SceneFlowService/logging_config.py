import logging
from pathlib import Path
import sys
from loguru import logger

# 单例式初始化标记
_INITIALIZED = False

class InterceptHandler(logging.Handler):
    def emit(self, record):
        # Get corresponding Loguru level if it exists
        try:
            level = logger.level(record.levelname).name
        except ValueError:
            level = record.levelno

        # Find caller from where originated the log
        frame, depth = logging.currentframe(), 2
        while frame.f_code.co_filename == logging.__file__:
            frame = frame.f_back
            depth += 1

        logger.opt(depth=depth, exception=record.exc_info).log(level, record.getMessage())

def setup_logging():
    global _INITIALIZED
    if _INITIALIZED:
        return logger
    log_dir = Path("logs")
    log_dir.mkdir(exist_ok=True)
    # 重置默认配置
    logger.remove()
    # 控制台输出
    logger.add(sys.stdout,
               level="INFO",
               format="[{time:YYYY-MM-DD HH:mm:ss.SSS}][{level}][{name}:{function}:{line}] {message}")
    # 文件日志（更详细，含线程进程）
    logger.add(log_dir / "app-{time:YYYY-MM-DD}.log",
               rotation="20 MB",
               retention="10 days",
               enqueue=True,
               encoding="utf-8",
               level="DEBUG",
               format="[{time:YYYY-MM-DD HH:mm:ss.SSS}][{level}][{process}.{thread}][{name}:{function}:{line}] {message}")

    logging.root.handlers = [InterceptHandler()]
    logging.root.setLevel(logging.WARNING)
    # logger_name_list = [name for name in logging.root.manager.loggerDict]
    # for logger_name in logger_name_list:
    #     print(f"Configuring logger: {logger_name}")
    #     _logger = logging.getLogger(logger_name)
    #     _logger.setLevel(logging.INFO)
    #     _logger.handlers = []
    #     if '.' not in logger_name:
    #         _logger.addHandler(InterceptHandler())

    _INITIALIZED = True
    return logger

# 模块导入即初始化，方便直接使用
setup_logging()

