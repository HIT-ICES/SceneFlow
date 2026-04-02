
def bin_len_to_str(bin_len: int) -> str:
    """
    将字节长度转换为更易读的字符串表示形式
    Args:
        bin_len (int): 字节长度
    Returns:
        str: 可读的字符串表示形式
    """
    if bin_len < 1024:
        return f"{bin_len} B"
    elif bin_len < 1024 ** 2:
        return f"{bin_len / 1024:.2f} KB"
    elif bin_len < 1024 ** 3:
        return f"{bin_len / (1024 ** 2):.2f} MB"
    else:
        return f"{bin_len / (1024 ** 3):.2f} GB"