import numpy as np
import matplotlib
from scipy.cluster import hierarchy

from utils import dataset

matplotlib.use('TkAgg')
import matplotlib.pyplot as plt
from sklearn.cluster import KMeans, DBSCAN
from mpl_toolkits.mplot3d import Axes3D
import json

np.random.seed(42)


# def scene_division(path: str, method: str, extra: dict = None) -> dict:
#     method = method.lower()
#     if method in ["dbscan", "hdbscan", "kmeans"]:
#         match (extra.get("source") or "voxel"):
#             case "voxel":
#                 raw_data, data_v, data_id = dataset.load_voxels(path,
#                                                                 downsample_step=extra.get("downsample_step") or 0.5)
#             case "vertical":
#                 raw_data, data_v, data_id = dataset.load_verticals(path,
#                                                                    downsample_step=extra.get("downsample_step") or 0.5)
#             case _:
#                 raise ValueError("Unsupported source type. Use 'voxel' or 'vertical'.")
#
#     if method == "DBSCAN" or method == "VOXELDBSCAN":
#         if method == "DBSCAN":
#             raw_data, data_v, data_id = dataset.load_verticals(params.scene_info_path,
#                                                            downsample_step=params.extra.get("downsample_step") or 0.5)
#         else:
#             raw_data, data_v, data_id = dataset.load_voxels(params.scene_info_path,
#                                                            downsample_step=params.extra.get("downsample_step") or 0.5)
#         labels = clusters.dbscan(data_v, data_id, eps=params.extra.get("eps") or 2,
#                                  min_samples=params.extra.get("min_samples") or 10, metric="euclidean")
#         result = dataset.group_object_by_labels(data_id, labels)
#     else:
#         raise ValueError("Unsupported method.")
#     return jsonable_encoder(result, custom_encoder={np.int64: int, np.float64: float})