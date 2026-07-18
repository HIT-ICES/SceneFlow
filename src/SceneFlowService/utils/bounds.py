"""
包围盒，以及用于处理包围盒相关的工具
"""
from typing import List

import numpy as np


class Bounds:
    center: np.ndarray
    extents: np.ndarray

    def __init__(self, center, extents):
        self.center = np.asarray(center)
        self.extents = np.asarray(extents)

    def min(self, axis=None):
        if axis is None:
            return self.center - self.extents
        return self.center[axis] - self.extents[axis]

    def max(self, axis=None):
        if axis is None:
            return self.center + self.extents
        return self.center[axis] + self.extents[axis]

    def __repr__(self):
        return f"Bounds(center={self.center}, extents={self.extents})"

    @staticmethod
    def from_vertices(vertices):
        """
        从顶点列表或ndarray创建Bounds对象
        """
        vertices = np.asarray(vertices)
        if vertices.ndim != 2 or vertices.shape[1] != 3:
            raise ValueError("顶点数组必须是形状为(N, 3)的ndarray或列表")
        if vertices.size == 0:
            raise ValueError("顶点数组不能为空")
        min_v = np.min(vertices, axis=0)
        max_v = np.max(vertices, axis=0)
        center = (min_v + max_v) / 2
        extents = (max_v - min_v) / 2
        return Bounds(center, extents)

    @staticmethod
    def intersect(bounds1: "Bounds", bounds2: "Bounds", *, axis: None | List[int] = None):
        """
        检查两个AABB包围盒是否相交
        """
        return np.all(bounds1.min(axis) <= bounds2.max(axis)) and np.all(bounds1.max(axis) >= bounds2.min(axis))

    @staticmethod
    def contains(bounds1: "Bounds", bounds2: "Bounds", *, axis: None | List[int] = None):
        """
        检查AABB1是否完全包含AABB2
        """
        return np.all(bounds1.min(axis) <= bounds2.min(axis)) and np.all(bounds1.max(axis) >= bounds2.max(axis))

