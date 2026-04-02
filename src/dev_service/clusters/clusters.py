from dataclasses import dataclass

import numpy as np
from utils import cluster_utils
from sklearn.cluster import DBSCAN
from loguru import logger
from typing import Literal

# @dataclass
# class ClusterParamsExtra:
#     trace_figure: bool = False
#     reduce_dim: bool = False
#     final





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

def pre_process(method_name, data_v, data_id, *, reduce_dim=True, trace_figure=False) -> np.ndarray:
    if trace_figure:
        cluster_utils.save_figure_data(f"Raw Data", data_v, np.zeros(len(data_v), dtype=int))
    data_v = data_v.copy()
    # # 预处理：将每个模型的点集向其中心拉近
    # for obj_id in np.unique(data_id):
    #     points = data_v[data_id == obj_id]
    #     center = points.mean(axis=0)
    #     # 用一个指向重心的向量拉近0.5
    #     data_v[data_id == obj_id] = points + (center - points) * 0.5
    return data_v if not reduce_dim else reduce_dimension(data_v)

def dbscan(data_v, data_id, *, reduce_dim=True, eps=2, min_samples=10, metric="euclidean",
           trace_figure=False) -> np.ndarray:
    """
    :return: labels
    """
    logger.info(f"dbscan: {len(data_v)} points, eps={eps}, min_samples={min_samples}, metric={metric}")

    data_v_preprocessed = pre_process("DBSCAN", data_v, data_id, reduce_dim=reduce_dim, trace_figure=trace_figure)

    dbscan_cluster = DBSCAN(eps=eps, min_samples=min_samples, metric=metric, n_jobs=16)
    labels = dbscan_cluster.fit_predict(data_v_preprocessed)
    logger.info(f"dbscan: dbscan predict find [{len(np.unique(labels))}] labels, [{np.sum(labels == -1)}] noise points")

    return post_process("DBSCAN", data_v, data_id, labels, trace_figure=trace_figure)


def kmeans(data_v, data_id, *, reduce_dim=True, n_clusters=2, trace_figure=False) -> np.ndarray:
    """
    :return: labels
    """
    logger.info(f"kmeans: {len(data_v)} points, n_clusters={n_clusters}")

    data_v_preprocessed = pre_process("DBSCAN", data_v, data_id, reduce_dim=reduce_dim, trace_figure=trace_figure)

    from sklearn.cluster import KMeans
    kmeans_cluster = KMeans(n_clusters=n_clusters)
    labels = kmeans_cluster.fit_predict(data_v_preprocessed)
    logger.info(f"kmeans: kmeans predict find [{len(np.unique(labels))}] labels")

    return post_process("KMEANS", data_v, data_id, labels, trace_figure=trace_figure)


def hdbscan(data_v, data_id, *, reduce_dim=True, ) -> np.ndarray:
    """
    Hierarchical DBSCAN clustering
    :return: labels
    """
    # logger.info(f"hdbscan: {len(data_v)} points, min_samples={min_samples}, metric={metric}")

    data_v_preprocessed = pre_process("HDBSCAN", data_v, data_id, reduce_dim=reduce_dim, trace_figure=False)

    from hdbscan import HDBSCAN
    hdbscan_cluster = HDBSCAN(min_cluster_size=300, min_samples=100, cluster_selection_method="eom")
    labels = hdbscan_cluster.fit_predict(data_v_preprocessed)
    logger.info(
        f"hdbscan: hdbscan predict find [{len(np.unique(labels))}] labels, [{np.sum(labels == -1)}] noise points")

    return post_process("HDBSCAN", data_v, data_id, labels, trace_figure=False)


def mean_shift(data_v, data_id, *, reduce_dim=True, bandwidth=None, trace_figure=False) -> np.ndarray:
    """
    Mean Shift clustering
    :return: labels
    """
    logger.info(f"MeanShift: {len(data_v)} points, bandwidth={bandwidth}")

    data_v_preprocessed = pre_process("MeanShift", data_v, data_id, reduce_dim=reduce_dim, trace_figure=trace_figure)

    from sklearn.cluster import MeanShift
    from sklearn.cluster import estimate_bandwidth

    if bandwidth is None:
        bandwidth = estimate_bandwidth(data_v_preprocessed, quantile=0.3, n_samples=len(data_v_preprocessed) // 10, n_jobs=16)
        logger.info(f"MeanShift: estimated bandwidth={bandwidth}")

    mean_shift_cluster = MeanShift(bandwidth=bandwidth, n_jobs=16)
    labels = mean_shift_cluster.fit_predict(data_v_preprocessed)
    logger.info(f"MeanShift: mean_shift predict find [{len(np.unique(labels))}] labels")

    return post_process("MeanShift", data_v, data_id, labels, trace_figure=trace_figure)


def agglomerative_clustering(
    data_v,
    data_id,
    *,
    reduce_dim=True,
    n_clusters=2,
    linkage: Literal["ward", "complete", "average", "single"] = "ward",
    metric="euclidean",
    trace_figure=False,
) -> np.ndarray:
    """
    Hierarchical Agglomerative clustering
    :return: labels
    """
    logger.info(
        f"AgglomerativeClustering: {len(data_v)} points, n_clusters={n_clusters}, linkage={linkage}, metric={metric}")

    data_v_preprocessed = pre_process("AgglomerativeClustering", data_v, data_id, reduce_dim=reduce_dim, trace_figure=trace_figure)

    from sklearn.cluster import AgglomerativeClustering

    # For "ward" linkage, sklearn requires Euclidean metric.
    if linkage == "ward":
        agglomerative_cluster = AgglomerativeClustering(n_clusters=n_clusters, linkage=linkage)
    else:
        agglomerative_cluster = AgglomerativeClustering(n_clusters=n_clusters, linkage=linkage, metric=metric)

    labels = agglomerative_cluster.fit_predict(data_v_preprocessed)
    logger.info(f"AgglomerativeClustering: predict find [{len(np.unique(labels))}] labels")

    return post_process("AgglomerativeClustering", data_v, data_id, labels, trace_figure=trace_figure)


def spectral_clustering(
    data_v,
    data_id,
    *,
    reduce_dim=True,
    n_clusters=2,
    gamma=1.0,
    affinity="rbf",
    n_neighbors=10,
    assign_labels: Literal["kmeans", "discretize", "cluster_qr"] = "kmeans",
    random_state=42,
    trace_figure=False,
) -> np.ndarray:
    """
    Spectral clustering
    :return: labels
    """
    logger.info(
        f"SpectralClustering: {len(data_v)} points, n_clusters={n_clusters}, affinity={affinity}, "
        f"gamma={gamma}, n_neighbors={n_neighbors}, assign_labels={assign_labels}, random_state={random_state}")

    data_v_preprocessed = pre_process("SpectralClustering", data_v, data_id, reduce_dim=reduce_dim, trace_figure=trace_figure)

    from sklearn.cluster import SpectralClustering

    spectral_cluster = SpectralClustering(
        n_clusters=n_clusters,
        affinity=affinity,
        gamma=gamma,
        n_neighbors=n_neighbors,
        assign_labels=assign_labels,
        random_state=random_state,
        n_jobs=16,
    )
    labels = spectral_cluster.fit_predict(data_v_preprocessed)
    logger.info(f"SpectralClustering: predict find [{len(np.unique(labels))}] labels")

    return post_process("SpectralClustering", data_v, data_id, labels, trace_figure=trace_figure)


def birch(data_v, data_id, *, reduce_dim=True, threshold=0.5, branching_factor=50,
          n_clusters=2, trace_figure=False) -> np.ndarray:
    """
    BIRCH clustering
    :return: labels
    """
    logger.info(
        f"BIRCH: {len(data_v)} points, threshold={threshold}, branching_factor={branching_factor}, "
        f"n_clusters={n_clusters}")

    data_v_preprocessed = pre_process("BIRCH", data_v, data_id, reduce_dim=reduce_dim, trace_figure=trace_figure)

    from sklearn.cluster import Birch

    birch_cluster = Birch(
        threshold=threshold,
        branching_factor=branching_factor,
        n_clusters=n_clusters,
    )
    labels = birch_cluster.fit_predict(data_v_preprocessed)
    logger.info(f"BIRCH: predict find [{len(np.unique(labels))}] labels")

    return post_process("BIRCH", data_v, data_id, labels, trace_figure=trace_figure)

def birch_auto(data_v, data_id, *, reduce_dim=True, threshold=0.5, branching_factor=50, trace_figure=False) -> np.ndarray:
    logger.info(
        f"BIRCH AUTO: {len(data_v)} points, threshold={threshold}, branching_factor={branching_factor}")

    data_v_preprocessed = pre_process("BIRCH_AUTO", data_v, data_id, reduce_dim=reduce_dim, trace_figure=trace_figure)

    from sklearn.cluster import Birch, AgglomerativeClustering, DBSCAN
    from sklearn.metrics import silhouette_score

    birch_cluster = Birch(
        threshold=threshold,
        branching_factor=branching_factor,
        n_clusters=None,
    )
    birch_cluster.fit(data_v_preprocessed)
    subcluster_centers = birch_cluster.subcluster_centers_

    dbscan = DBSCAN(eps=threshold * 2, min_samples=5)
    sub_labels = dbscan.fit_predict(subcluster_centers)
    labels = sub_labels[birch_cluster.labels_]

    # # Step 2: 在子簇中心上寻找最佳 K 值
    # best_k = 2
    # max_score = -1
    # for k in range(2, 11):
    #     model = AgglomerativeClustering(n_clusters=k)
    #     labels = model.fit_predict(subcluster_centers)
    #     score = silhouette_score(subcluster_centers, labels)
    #     if score > max_score:
    #         max_score = score
    #         best_k = k
    #
    # logger.info(f"BIRCH AUTO: best K found is {best_k} with silhouette score {max_score}")
    #
    # birch_cluster.n_clusters = best_k
    # birch_cluster.partial_fit(data_v_reduced)
    # labels = birch_cluster.predict(data_v_reduced)

    logger.info(f"BIRCH AUTO: predict find [{len(np.unique(labels))}] labels")

    return post_process("BIRCH AUTO", data_v, data_id, labels, trace_figure=trace_figure)

def grid(data_v, data_id, *, num=None, trace_figure=False) -> np.ndarray:
    if num is None:
        num = np.array([8, 1, 8])
    logger.info(f"GRID: {len(data_v)} points, num={num}")
    data_min = data_v.min(axis=0)
    data_max = data_v.max(axis=0)
    data_len = data_max - data_min
    data_v_normalized = (data_v - data_min) / data_len
    grid_size = 1.0 / num
    grid_indices = np.floor(data_v_normalized / grid_size).astype(int)
    grid_indices = np.clip(grid_indices, 0, num - 1)
    labels = grid_indices[:, 0] * (num[1] * num[2]) + grid_indices[:, 1] * num[2] + grid_indices[:, 2]
    logger.info(f"GRID: grid predict find [{len(np.unique(labels))}] labels")
    return post_process("GRID", data_v, data_id, labels, trace_figure=trace_figure)


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
