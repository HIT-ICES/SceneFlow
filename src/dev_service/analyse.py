import os

import matplotlib
import numpy as np
from matplotlib import pyplot as plt
from sklearn.neighbors import NearestNeighbors

import clusters
from utils import dataset, cluster_utils
from utils.bounds import Bounds
from utils.dataset import load_scene_json_file
import logging_config  # noqa: F401

matplotlib.use('QtAgg')


def iter_group(list, n):
    """
    将列表分组，每组n个元素
    """
    for i in range(0, len(list), n):
        yield list[i:i + n]


def xxx(data_v, k):
    neighbors = NearestNeighbors(n_neighbors=k)
    neighbors_fit = neighbors.fit(data_v)
    distances, indices = neighbors_fit.kneighbors(data_v)
    distances = np.sort(distances[:, -1])  # 取每个点到第5近邻的距离

    plt.plot(distances)
    plt.title("K-Distance Graph")
    plt.xlabel("Points sorted by distance")
    plt.ylabel("5th Nearest Neighbor Distance")
    plt.show()


# def test_dbscan()

def main():
    if os.listdir("figure_cache"):
        cluster_utils.show_figure_saved()
        # cluster_utils.delete_figure_saved()
        return
    # raw_data = load_scene_json_file("data/apartments.gobj_info.json")
    raw_data = load_scene_json_file("data/classroom.gobj_info.json")
    # raw_data = load_scene_json_file("data/bistro.gobj_info.json")
    data_v, data_id = dataset.load_voxels(raw_data, downsample_step=0.5)
    # raw_data, data_v, data_id = dataset.load_voxels("data/hogwarts.gobj_info.json", downsample_step=1)
    # raw_data, data_v, data_id = dataset.load_voxels("data/classroom.gobj_info.json", downsample_step=0.5)
    # raw_data, data_v, data_id = dataset.load_verticals("data/apartments.gobj_info.json", downsample_step=0.5)
    # raw_data, data_v, obj_v_idxes = dataset.load_voxels_new("data/apartments.gobj_info.json", downsample_step=0.5)

    # xxx(data_v, 50)

    labels = clusters.dbscan(data_v, data_id, reduce_dim=True, eps=2, min_samples=100, trace_figure=True)

    cluster_utils.show_figure_saved()

    # labels = clusters.dbscan_new(data_v, obj_v_idxes, reduce_dim=True, eps=2, min_samples=75, trace_figure=True)
    # labels = clusters.hdbscan(data_v_2d, data_id)
    # labels = clusters.kmeans(data_v, data_id, n_clusters=5)

    # mlab.figure()
    # mlab.points3d(data_v[:, 0], data_v[:, 1], data_v[:, 2], labels, mode='point', scale_factor=5.0, colormap="spectral")
    # mlab.title("DBSCAN Result")
    # mlab.show()
    #
    # unique_labels = np.unique(labels)
    # bounds = [Bounds.from_vertices(data_v[labels == lb]) for lb in range(0, len(unique_labels))]
    # contains_relation = []
    # for i in range(len(bounds)):
    #     for j in range(len(bounds)):
    #         if i == j:
    #             continue
    #         if Bounds.contains(bounds[i], bounds[j], axis=[0, 2]):
    #             contains_relation.append((i, j))
    #
    # print(f"在{len(bounds)}个子场景中，包含关系的数量为: {len(contains_relation)}")
    #
    # for group in iter_group(contains_relation, 5):
    #     for a, b in group:
    #         print(f"Bounds {a} contains Bounds {b}:")
    #         mask = (labels == a) | (labels == b)
    #         data_v_ab = data_v[mask]
    #         labels_ab = labels[mask]
    #         mlab.figure()
    #         mlab.points3d(data_v_ab[:, 0], data_v_ab[:, 1], data_v_ab[:, 2], labels_ab, mode='point', scale_factor=5.0,
    #                       colormap="spectral")
    #     mlab.show()
    # 统计每个类别的样本数（不包括噪声点，噪声点label为-1）
    # unique_labels, counts = np.unique(labels[labels != -1], return_counts=True)
    #
    # # 找到样本量最大的前20个类别
    # top20_indices = np.argsort(counts)[-50:]  # 取最大的20个类别的索引
    # top20_labels = unique_labels[top20_indices]
    #
    # # 只保留属于这20个类别的点
    # mask = np.isin(labels, top20_labels)
    # filtered_data = data_v[mask]
    # filtered_labels = labels[mask]
    #
    # print("Number of clusters (top 20):", len(top20_labels))
    #
    # # 重新映射label为0~19，便于colormap显示
    # label_map = {label: idx for idx, label in enumerate(top20_labels)}
    # filtered_labels_mapped = np.array([label_map[l] for l in filtered_labels])
    #
    # mlab.points3d(filtered_data[:, 0], filtered_data[:, 1], filtered_data[:, 2],
    #               filtered_labels_mapped, mode='point', scale_factor=5.0, colormap="spectral")
    # mlab.show()


if __name__ == "__main__":
    main()
