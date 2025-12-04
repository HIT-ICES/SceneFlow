import numpy as np
from utils import cluster_utils
from sklearn.cluster import DBSCAN
from loguru import logger


def reduce_dimension(data_v: np.ndarray) -> np.ndarray:
    """
    Reduce the dimensionality of the data to 2D by selecting the first two dimensions.
    :param data_v: The original data points in 3D.
    :return: Reduced data points in 2D.
    """
    return data_v[:, [0, 2]]  # Select only x and z dimensions for 2D representation


def post_process(method_name, data_v, data_id, labels, *, trace_figure=False) -> np.ndarray:
    if trace_figure:
        cluster_utils.save_figure_data(f"{method_name} Raw Result", data_v, labels)
        mask = labels != -1
        cluster_utils.save_figure_data(f"{method_name} Result Without Noise", data_v[mask, :], labels[mask])

    labels = cluster_utils.cluster_noise_points(data_v=data_v, labels=labels)
    logger.info(f"{method_name}: after cluster_noise_points, [{np.sum(labels == -1)}] noise points")
    if trace_figure:
        cluster_utils.save_figure_data(f"{method_name} Result After Cluster Noise", data_v, labels)

    labels = cluster_utils.unify_by_id(data_id, labels)
    logger.info(
        f"{method_name}: after unify_by_id, [{len(np.unique(labels))}] labels, [{np.sum(labels == -1)}] noise points")
    if trace_figure:
        cluster_utils.save_figure_data(f"{method_name} Result After Unify", data_v, labels)

    labels = cluster_utils.remap_labels(labels)
    return labels


def dbscan(data_v, data_id, *, reduce_dim=True, eps=2, min_samples=10, metric="euclidean",
           trace_figure=False) -> np.ndarray:
    """
    :return: labels
    """
    logger.info(f"dbscan: {len(data_v)} points, eps={eps}, min_samples={min_samples}, metric={metric}")

    if trace_figure:
        cluster_utils.save_figure_data(f"Raw Data", data_v, np.zeros(len(data_v), dtype=int))

    dbscan_cluster = DBSCAN(eps=eps, min_samples=min_samples, metric=metric, n_jobs=16)
    labels = dbscan_cluster.fit_predict(data_v if not reduce_dim else reduce_dimension(data_v))
    logger.info(f"dbscan: dbscan predict find [{len(np.unique(labels))}] labels, [{np.sum(labels == -1)}] noise points")

    return post_process("DBSCAN", data_v, data_id, labels, trace_figure=trace_figure)


def kmeans(data_v, data_id, *, reduce_dim=True, n_clusters=2) -> np.ndarray:
    """
    :return: labels
    """
    logger.info(f"kmeans: {len(data_v)} points, n_clusters={n_clusters}")

    from sklearn.cluster import KMeans
    kmeans_cluster = KMeans(n_clusters=n_clusters)
    labels = kmeans_cluster.fit_predict(data_v if not reduce_dim else reduce_dimension(data_v))
    print(f"kmeans: kmeans predict find [{len(np.unique(labels))}] labels")

    return post_process("KMEANS", data_v, data_id, labels, trace_figure=False)


def hdbscan(data_v, data_id, *, reduce_dim=True, ) -> np.ndarray:
    """
    Hierarchical DBSCAN clustering
    :return: labels
    """
    # logger.info(f"hdbscan: {len(data_v)} points, min_samples={min_samples}, metric={metric}")

    from hdbscan import HDBSCAN
    hdbscan_cluster = HDBSCAN(min_cluster_size=300, min_samples=100, cluster_selection_method="eom")
    labels = hdbscan_cluster.fit_predict(data_v if not reduce_dim else reduce_dimension(data_v))
    logger.info(
        f"hdbscan: hdbscan predict find [{len(np.unique(labels))}] labels, [{np.sum(labels == -1)}] noise points")

    return post_process("HDBSCAN", data_v, data_id, labels, trace_figure=False)


def mean_shift(data_v, data_id, *, reduce_dim=True, bandwidth=None) -> np.ndarray:
    """
    Mean Shift clustering
    :return: labels
    """
    logger.info(f"MeanShift: {len(data_v)} points, bandwidth={bandwidth}")

    from sklearn.cluster import MeanShift
    from sklearn.cluster import estimate_bandwidth
    data_v_reduced = data_v if not reduce_dim else reduce_dimension(data_v)
    if bandwidth is None:
        bandwidth = estimate_bandwidth(data_v_reduced, quantile=0.3, n_samples=len(data_v_reduced) // 10, n_jobs=16)
        logger.info(f"MeanShift: estimated bandwidth={bandwidth}")

    mean_shift_cluster = MeanShift(bandwidth=bandwidth, n_jobs=16)
    labels = mean_shift_cluster.fit_predict(data_v_reduced)
    logger.info(f"MeanShift: mean_shift predict find [{len(np.unique(labels))}] labels")

    return post_process("MeanShift", data_v, data_id, labels, trace_figure=False)


def post_process_new(method_name, data_v, obj_v_idxes, labels, *, trace_figure=False) -> np.ndarray:
    if trace_figure:
        cluster_utils.save_figure_data(f"{method_name} Raw Result", data_v, labels)
        mask = labels != -1
        cluster_utils.save_figure_data(f"{method_name} Result Without Noise", data_v[mask, :], labels[mask])

    labels = cluster_utils.cluster_noise_points(data_v=data_v, labels=labels)
    logger.info(f"{method_name}: after cluster_noise_points, [{np.sum(labels == -1)}] noise points")
    if trace_figure:
        cluster_utils.save_figure_data(f"{method_name} Result After Cluster Noise", data_v, labels)

    labels = cluster_utils.unify_by_id_new(obj_v_idxes, labels)
    logger.info(
        f"{method_name}: after unify_by_id, [{len(np.unique(labels))}] labels, [{np.sum(labels == -1)}] noise points")
    if trace_figure:
        cluster_utils.save_figure_data(f"{method_name} Result After Unify", data_v, labels)

    labels = cluster_utils.remap_labels(labels)
    return labels


def dbscan_new(data_v, obj_v_idxes, *, reduce_dim=True, eps=2, min_samples=10, metric="euclidean",
               trace_figure=False) -> np.ndarray:
    """
    :return: labels
    """
    logger.info(f"dbscan: {len(data_v)} points, eps={eps}, min_samples={min_samples}, metric={metric}")

    dbscan_cluster = DBSCAN(eps=eps, min_samples=min_samples, metric=metric)
    labels = dbscan_cluster.fit_predict(data_v if not reduce_dim else reduce_dimension(data_v))
    logger.info(f"dbscan: dbscan predict find [{len(np.unique(labels))}] labels, [{np.sum(labels == -1)}] noise points")

    return post_process_new("DBSCAN", data_v, obj_v_idxes, labels, trace_figure=trace_figure)
