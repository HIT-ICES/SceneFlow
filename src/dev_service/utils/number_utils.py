
def bin_len_to_str(bin_len: int) -> str:
    """
    Convert a byte length into a more human-readable string representation.
    """
    if bin_len < 1024:
        return f"{bin_len} B"
    elif bin_len < 1024 ** 2:
        return f"{bin_len / 1024:.2f} KB"
    elif bin_len < 1024 ** 3:
        return f"{bin_len / (1024 ** 2):.2f} MB"
    else:
        return f"{bin_len / (1024 ** 3):.2f} GB"