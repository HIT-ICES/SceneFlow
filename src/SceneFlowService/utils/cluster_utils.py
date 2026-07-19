import os
import uuid
from datetime import datetime
from typing import Any

import numpy as np
from loguru import logger
import pickle
from sklearn.neighbors import NearestNeighbors

FIGURE_CACHE_PATH = "figures/cache"


def downsample_data(data, step=0.5):
    """
    Downsample data using step as the sampling interval.
    """
    data = np.array(data)
    keys = (data / step).round().astype(int)
    _, idx, ds_inverse = np.unique(keys, axis=0, return_index=True, return_inverse=True)
    data_ds = data[idx]
    return data_ds


def unify_by_id(data_id_list, labels):
    """
    Assign one label to each ID, choosing the most frequent label when necessary.
    """
    id_label_bucket = {}
    for vid, label in zip(data_id_list, labels):
        if vid not in id_label_bucket:
            id_label_bucket[vid] = {}
        if label not in id_label_bucket[vid]:
            id_label_bucket[vid][label] = 0
        id_label_bucket[vid][label] += 1

    id_label_map = {}
    for vid, label_dict in id_label_bucket.items():
        # Find the most frequent label.
        max_label = max(label_dict, key=label_dict.get)
        id_label_map[vid] = max_label

    return np.array([
        id_label_map[x] for x in data_id_list
    ])


def cluster_noise_points(*, data_v, labels):
    """
    Replace each -1 label with the label of the nearest point whose label is not -1.
    Args:
        data_v (np.ndarray): shape (n_samples, n_features)
        labels (np.ndarray): shape (n_samples,)
    Returns:
        np.ndarray: Updated label array.
    """
    data_v = np.asarray(data_v)
    labels = np.asarray(labels)
    new_labels = labels.copy()

    # Consider only samples whose label is not -1.
    mask_valid = labels != -1
    mask_invalid = labels == -1

    # Return immediately if no labels need updating.
    if not np.any(mask_invalid):
        return new_labels

    # Build the nearest-neighbor model.
    nbrs = NearestNeighbors(n_neighbors=1, algorithm='auto', n_jobs=16).fit(data_v[mask_valid])
    distances, indices = nbrs.kneighbors(data_v[mask_invalid])

    # Update the labels.
    nearest_labels = labels[mask_valid][indices[:, 0]]
    new_labels[mask_invalid] = nearest_labels

    return new_labels


def remap_labels(labels):
    """
    Remap labels to a zero-based range.
    """
    unique_labels = np.unique(labels)
    label_map = {label: i for i, label in enumerate(unique_labels)}
    return np.array([label_map[label] for label in labels])


def labels_to_colors(labels, colormap="tab20"):
    """
    Map labels to colors.
    """
    import matplotlib.pyplot as plt  # Import lazily.
    unique_labels = np.unique(labels)
    if type(colormap) == str:
        colormap = plt.get_cmap(colormap, len(unique_labels))
    label_to_color = {label: colormap(i)[:3] for i, label in enumerate(unique_labels)}
    colors = np.array([label_to_color[label] for label in labels])
    return colors


def _save_figure_image_matplotlib(data_v, labels, *, filename, point_size=3, colormap="tab20"):
    import matplotlib.pyplot as plt

    colors = labels_to_colors(labels, colormap)
    fig, ax = plt.subplots(figsize=(8, 8), dpi=160)
    ax.scatter(
        data_v[:, 0],
        data_v[:, 2],
        c=colors,
        s=max(0.1, float(point_size) * 0.25),
        marker=",",
        linewidths=0,
        rasterized=True,
    )
    ax.set_aspect("equal", adjustable="box")
    ax.axis("off")
    fig.tight_layout(pad=0)
    fig.savefig(filename, bbox_inches="tight", pad_inches=0)
    plt.close(fig)
    print(f"Open3D不可用，已使用Matplotlib保存XZ投影视图: {filename}")


def show_figure(title, data_v, labels, point_size=1, colormap="tab20", non_modal=False, save_pic_path=None):
    # Import Open3D lazily to avoid consuming memory when it is unused.
    import open3d as o3d
    if non_modal:
        logger.info("show_figure: non-modal mode, start a new process")
        from multiprocessing import Process
        p = Process(target=show_figure, args=(title, data_v, labels, point_size, colormap, False, save_pic_path))
        p.start()
        return
    logger.info(f"show_figure: {len(data_v)} points, title={title}")
    pcd = o3d.geometry.PointCloud()
    pcd.points = o3d.utility.Vector3dVector(data_v)
    pcd.colors = o3d.utility.Vector3dVector(labels_to_colors(labels, colormap))
    # Compute the centroid.
    center = np.mean(data_v, axis=0)
    o3d.visualization.draw(
        [pcd],
        point_size=int(point_size),
        title=title,
        width=1024,
        height=1024,
        lookat=center,
        eye=np.array([200, 200, 0]),
        up=np.array([0, 1, 0])
    )


def save_figure_image(data_v, labels, *, filename, point_size=3, colormap="tab20"):
    import open3d as o3d
    width = 1024
    height = 1024
    eye = np.array([200.0, 200.0, 0.0])
    up = np.array([0.0, 1.0, 0.0])
    center = np.mean(data_v, axis=0)
    # Construct the point-cloud object.
    pcd = o3d.geometry.PointCloud()
    pcd.points = o3d.utility.Vector3dVector(data_v)
    pcd.colors = o3d.utility.Vector3dVector(labels_to_colors(labels, colormap))

    # 1. Initialize the legacy Visualizer with a hidden window (visible=False).
    vis = o3d.visualization.Visualizer()
    if not vis.create_window(window_name='Hidden Render', width=width, height=height, visible=False):
        vis.destroy_window()
        _save_figure_image_matplotlib(data_v, labels, filename=filename, point_size=point_size, colormap=colormap)
        return

    # 2. Add the point cloud to the rendering scene.
    vis.add_geometry(pcd)

    # 3. Configure rendering options corresponding to the previous Material settings.
    opt = vis.get_render_option()
    if opt is None:
        vis.destroy_window()
        _save_figure_image_matplotlib(data_v, labels, filename=filename, point_size=point_size, colormap=colormap)
        return
    opt.point_size = float(point_size)
    # opt.background_color = np.asarray([1.0, 1.0, 1.0]) # The default background is black; uncomment for white.

    # 4. Configure the camera view.
    ctr = vis.get_view_control()

    # Convert the absolute eye position to the front direction vector expected by the camera.
    front = eye - center
    front_norm = np.linalg.norm(front)
    if front_norm > 0:
        front = front / front_norm

    ctr.set_lookat(center)
    ctr.set_front(front)
    ctr.set_up(up)

    # The legacy viewer uses zoom to control distance, similarly to the previous FOV setting.
    # 0.8 is empirical; decrease it (for example, to 0.5) for a larger point cloud or increase it (for example, to 1.2) for a smaller one.
    ctr.set_zoom(0.5)

    # 5. Render and capture the image data.
    vis.poll_events()
    vis.update_renderer()

    # 6. Save the image.
    vis.capture_screen_image(filename)

    # 7. Destroy the window and release its resources.
    vis.destroy_window()
    print(f"渲染完成，图片已保存为: {filename}")

# def save_figure_image(data_v, labels, *, filename, point_size=1, colormap="tab20"):
#     import open3d as o3d
#     width = 1024
#     height = 1024
#     eye = np.array([200, 200, 0])
#     up = np.array([0, 1, 0])
#     center = np.mean(data_v, axis=0)
#
#     pcd = o3d.geometry.PointCloud()
#     pcd.points = o3d.utility.Vector3dVector(data_v)
#     pcd.colors = o3d.utility.Vector3dVector(labels_to_colors(labels, colormap))
#
#     # 1. Initialize the off-screen renderer.
#     render = o3d.visualization.rendering.OffscreenRenderer(width, height)
#
#     # 2. Configure the material to match the point_size setting above.
#     material = o3d.visualization.rendering.MaterialRecord()
#     material.shader = "defaultUnlit"  # Use "defaultLit" when the point cloud has normals and requires lighting.
#     material.point_size = float(point_size)
#
#     # 3. Add the point cloud to the rendering scene.
#     render.scene.add_geometry("pcd", pcd, material)
#
#     # 4. Optionally set the background color; this value is opaque white (RGBA).
#     # render.scene.set_background([1.0, 1.0, 1.0, 1.0])
#
#     # 5. Configure the camera view.
#     # Parameters: vertical field of view (FOV), lookat (center), eye, and up.
#     fov = 60.0
#     render.setup_camera(fov, center, eye, up)
#
#     # 6. Render and capture the image data.
#     img = render.render_to_image()
#
#     # 7. Save the image.
#     o3d.io.write_image(filename, img)
#     print(f"Rendering complete; image saved to: {filename}")



def show_figure_saved(point_size=5, colormap="tab20"):
    """
    Render previously saved figure data.
    """
    files = [f for f in os.listdir(FIGURE_CACHE_PATH) if f.endswith('.pkl')]
    if not files:
        logger.info("No figure data found.")
        return

    for file in files:
        if not file.endswith('.pkl'):
            continue
        with open(os.path.join(FIGURE_CACHE_PATH, file), 'rb') as f:
            data = pickle.load(f)
            data_v = data['data_v']
            labels = data['labels']
            title = data.get('title', None)
            save_pic_path = os.path.join(FIGURE_CACHE_PATH, file.replace('.pkl', '.png'))
            show_figure(title, data_v, labels, point_size, colormap, True, save_pic_path)


def delete_figure_saved():
    """
    Delete saved figure data.
    """
    files = [f for f in os.listdir(FIGURE_CACHE_PATH) if f.endswith('.pkl')]
    for file in files:
        os.remove(os.path.join(FIGURE_CACHE_PATH, file))
    logger.info("Deleted all saved figure data.")


def save_figure_data(title, data_v, labels):
    """
    Save figure data to a pickle file.
    """
    now = datetime.now()
    filename = now.strftime('%Y-%m-%d-%H-%M-%S') + '-%03d' % (now.microsecond // 1000) + uuid.uuid4().hex + '.pkl'
    path = f"{FIGURE_CACHE_PATH}/{filename}"
    if not os.path.exists(FIGURE_CACHE_PATH):
        os.makedirs(FIGURE_CACHE_PATH)
    with open(path, 'wb') as f:
        pickle.dump({
            'title': title,
            'data_v': data_v,
            'labels': labels,
        }, f)


def downsample_data_new(data_v: np.ndarray, data_id: np.ndarray, step=0.5) -> tuple[np.ndarray, dict[Any, np.ndarray]]:
    """
    Downsample data using step as the sampling interval.
    """
    keys = (data_v / step).astype(int)
    _, data_v_idx, data_ds_inverse = np.unique(keys, axis=0, return_index=True, return_inverse=True)

    data_id_u, data_id_inverse = np.unique(data_id, return_inverse=True)
    data_id_idx = [[] for _ in range(len(data_id_u))]
    for i, id_idx in enumerate(data_id_inverse):
        data_id_idx[id_idx].append(i)

    obj_point_indexes = {}
    for i, did in enumerate(data_id_u):
        obj_point_indexes[did] = np.unique(data_ds_inverse[data_id_idx[i]])
    return data_v[data_v_idx], obj_point_indexes


def unify_by_id_new(obj_v_idxes: dict, labels: np.ndarray):
    """
    Assign one label to each ID, choosing the most frequent label when necessary.
    """
    new_labels = np.array(labels, copy=True)
    for oid, v_idxes in obj_v_idxes.items():
        label_counts = {}
        for idx in v_idxes:
            label = labels[idx]
            if label not in label_counts:
                label_counts[label] = 0
            label_counts[label] += 1
        # Find the most frequent label.
        max_label = max(label_counts, key=label_counts.get)
        for idx in v_idxes:
            new_labels[idx] = max_label
    return new_labels


def test_downsample_data_new():
    ds, idx = downsample_data_new(np.array([0, 0.5, 1, 1.5, 2, 2.5]), np.array(["a", "a", "a", "b", "b", "b"]), step=1)
    print("Downsampled Data:", ds)
    print("Index Mapping:", idx)
    assert np.array_equal(ds, np.array([0, 1, 2]))
    assert np.array_equal(idx["a"], np.array([0, 1]))
    assert np.array_equal(idx["b"], np.array([1, 2]))
