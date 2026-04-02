import json
import orjson
import numpy as np

from utils import cluster_utils
from loguru import logger


def load_scene_json_file(path: str) -> dict:
    # with open(path, "r", encoding="utf-8") as f:
    #     raw_data = json.load(f)
    with open(path, "rb") as f:
        raw_data = orjson.loads(f.read())
    return raw_data


def load_verticals(raw_data: dict, downsample_step=0.5) -> tuple[np.ndarray, np.ndarray]:
    """
    :return: (raw_data, data_verticals, data_id)
    """
    logger.info(f"load: [{len(raw_data['objects'])}] objects")

    data = [
        [cluster_utils.downsample_data(x["vertices"], downsample_step), x["id"]]
        for x in raw_data["objects"]
        if x["vertices"] is not None
    ]

    data = [
        [vertex, vid]
        for [vertex_list, vid] in data
        for vertex in vertex_list
    ]

    data_v = np.array([v for [v, _] in data])
    data_id = np.array([vid for [_, vid] in data])

    logger.info(f"load: load [{len(data)}] vertice after downsample")

    return data_v, data_id


def load_voxels(raw_data: dict, downsample_step=0.5) -> tuple[np.ndarray, np.ndarray]:
    """
    :return: (raw_data, data_verticals, data_id)
    """
    logger.info(f"load: [{len(raw_data['objects'])}] objects")
    logger.info(f"voxels: {raw_data['objects'][0]['voxels'][:10]}")  
    data = [
        [cluster_utils.downsample_data(x["voxels"], downsample_step), x["id"]]
        for x in raw_data["objects"]
        if x["voxels"] is not None
    ]

    data = [
        [voxel, vid]
        for [voxel_list, vid] in data
        for voxel in voxel_list
    ]

    data_v = np.array([v for [v, _] in data])
    data_id = np.array([vid for [_, vid] in data])

    logger.info(f"load: load [{len(data)}] vertice after downsample")

    return data_v, data_id


def load_voxels_new(raw_data: dict, downsample_step=0.5) -> tuple[np.ndarray, dict]:
    """
    :return: (raw_data, data_verticals, data_id)
    """
    logger.info(f"load: [{len(raw_data['objects'])}] objects")
    logger.info(f"voxels: {raw_data['objects'][0]['voxels'][:10]}")  
    data = [
        [x["voxels"], x["id"]]
        for x in raw_data["objects"]
        if x["voxels"] is not None
    ]

    data = [
        [voxel, vid]
        for [voxel_list, vid] in data
        for voxel in voxel_list
    ]

    data_v = np.array([v for [v, _] in data])
    data_id = np.array([vid for [_, vid] in data])

    logger.info(f"load: load [{len(data_v)}] points")

    data_v, obj_point_indexes = cluster_utils.downsample_data_new(data_v, data_id, step=downsample_step)

    logger.info(f"load: downsample to [{len(data_v)}] points")

    return data_v, obj_point_indexes


def find_object_labels(data_id: np.ndarray, labels: np.ndarray) -> dict:
    """
    Build a mapping from object IDs to labels using vertex IDs and vertex labels.
    :param data_id: Vertex IDs
    :param labels: Label for each vertex
    :return: A dictionary where keys are vertex IDs and values are labels
    """
    id_label_map = {}
    for vid, label in zip(data_id, labels):
        if vid not in id_label_map:
            id_label_map[vid] = label
        elif id_label_map[vid] != label:
            raise ValueError(f"Vertex ID {vid} maps to multiple labels: {id_label_map[vid]} and {label}")
    return id_label_map


def group_object_by_labels(data_id: np.ndarray, labels: np.ndarray) -> list:
    """
    Group object IDs by label using vertex IDs and vertex labels.
    """
    id_label_map = find_object_labels(data_id, labels)
    label_groups = {}
    for vid, label in id_label_map.items():
        if label not in label_groups:
            label_groups[label] = []
        label_groups[label].append(vid)
    return [label_groups[label] for label in sorted(label_groups.keys())]
