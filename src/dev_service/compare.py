import os
from pathlib import Path

import clusters
import utils.cluster_utils
from utils.dataset import load_scene_json_file
import utils.dataset as dataset_utils
import numpy as np

pic_path = Path("./figures/compare")
datasets = (
    ("classroom", "data/classroom.gobj_info.json"),
    ("apartments", "data/apartments.gobj_info.json"),
    ("bistro", "data/bistro.gobj_info.json"),
)

loaders = ("voxels", "verticals")

methods = ("dbscan", "kmeans", "birch")


def save_result(context):
    name_keys = (
        ("data_name", lambda x: x),
        ("method", lambda x: x),
        ("loader", lambda x: x),
        ("reduce_dim", lambda x: "reduced" if x else "unreduced"),
        ("n_clusters", lambda x: f"k={x}"),
        ("threshold", lambda x: f"th={x}"),
    )
    names = [f(context[k]) for (k, f) in name_keys if context.get(k) is not None]
    path = pic_path / ("-".join(names) + ".png")
    print(f"labels: {np.unique(context["labels"])}")
    utils.cluster_utils.save_figure_image(context["data_v"], context["labels"],
                                          filename=path)
    # utils.cluster_utils.show_figure("-".join(names), context["data_v"], context["labels"], non_modal=True)


def compare_dbscan(context):
    context["method"] = "dbscan"
    context["labels"] = clusters.dbscan(context["data_v"], context["data_id"], reduce_dim=context["reduce_dim"], eps=2,
                                        min_samples=100)
    save_result(context)


def compare_kmeans(context):
    context["method"] = "kmeans"
    for n_clusters in range(3, 6):
        context["n_clusters"] = n_clusters
        context["labels"] = clusters.kmeans(context["data_v"], context["data_id"], n_clusters=n_clusters,
                                            reduce_dim=context["reduce_dim"])
        save_result(context)
    del context["n_clusters"]


def compare_birch(context):
    context["method"] = "birch"
    for n_clusters in range(3, 6):
        for threshold in [0.5, 1.0, 1.5, 2.0, 3.0]:
            context["n_clusters"] = n_clusters
            context["threshold"] = threshold
            context["labels"] = clusters.birch(context["data_v"], context["data_id"], n_clusters=n_clusters, threshold=threshold,
                                               reduce_dim=context["reduce_dim"])
            save_result(context)
    del context["n_clusters"]
    del context["threshold"]


def compare_methods(context):
    for reduce_dim in [True, False]:
        context["reduce_dim"] = reduce_dim
        # compare_dbscan(context)
        # compare_kmeans(context)
        compare_birch(context)


def compare_loaders(context):
    context["loader"] = "voxels"
    context["data_v"], context["data_id"] = dataset_utils.load_voxels(context["raw_data"], downsample_step=0.5)
    compare_methods(context)

    context["loader"] = "verticals"
    context["data_v"], context["data_id"] = dataset_utils.load_verticals(context["raw_data"], downsample_step=0.5)
    compare_methods(context)


def compare_datasets():
    context = {}
    for data_name, data_path in datasets:
        context["data_name"] = data_name
        context["data_path"] = data_path
        raw_data = load_scene_json_file(data_path)
        context["raw_data"] = raw_data
        compare_loaders(context)


def main():
    os.makedirs(pic_path, exist_ok=True)
    # delete all pics
    for file in os.listdir(pic_path):
        if file.endswith(".png"):
            os.remove(pic_path / file)
    compare_datasets()
    return
    for data_name, data_path in datasets:
        raw_data = load_scene_json_file(data_path)
        for loader in loaders:
            if loader == "voxels":
                data_v, data_id = dataset_utils.load_voxels(raw_data, downsample_step=0.5)
            elif loader == "verticals":
                data_v, data_id = dataset_utils.load_verticals(raw_data, downsample_step=0.5)
            else:
                raise NotImplementedError
            for method in methods:
                for reduce_dim in [True, False]:
                    if method == "dbscan":
                        labels = clusters.dbscan(data_v, data_id, reduce_dim=reduce_dim, eps=2, min_samples=100,
                                                 trace_figure=True)
                    elif method == "kmeans":
                        labels = clusters.kmeans(data_v, data_id, n_clusters=5, reduce_dim=reduce_dim,
                                                 trace_figure=True)
                    elif method == "birch":
                        labels = clusters.birch(data_v, data_id, n_clusters=4, threshold=2.0, reduce_dim=reduce_dim,
                                                trace_figure=True)
                    else:
                        raise NotImplementedError
                    utils.cluster_utils.save_figure_image(data_v, labels,
                                                          filename=pic_path / f"{data_name}_{loader}_{method}_{'reduced' if reduce_dim else 'original'}.png")


if __name__ == "__main__":
    main()
