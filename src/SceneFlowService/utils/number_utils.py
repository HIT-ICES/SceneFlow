
def bin_len_to_str(bin_len: int) -> str:
    """
    Convert a byte count to a human-readable string.
    Args:
        bin_len (int): Byte count.
    Returns:
        str: Human-readable representation.
    """
    if bin_len < 1024:
        return f"{bin_len} B"
    elif bin_len < 1024 ** 2:
        return f"{bin_len / 1024:.2f} KB"
    elif bin_len < 1024 ** 3:
        return f"{bin_len / (1024 ** 2):.2f} MB"
    else:
        return f"{bin_len / (1024 ** 3):.2f} GB"
