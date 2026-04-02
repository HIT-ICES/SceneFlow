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
        Create a Bounds object from a vertex list or ndarray.
        """
        vertices = np.asarray(vertices)
        if vertices.ndim != 2 or vertices.shape[1] != 3:
            raise ValueError("(N, 3)")
        if vertices.size == 0:
            raise ValueError("empty vertices")
        min_v = np.min(vertices, axis=0)
        max_v = np.max(vertices, axis=0)
        center = (min_v + max_v) / 2
        extents = (max_v - min_v) / 2
        return Bounds(center, extents)

    @staticmethod
    def intersect(bounds1: "Bounds", bounds2: "Bounds", *, axis: None | List[int] = None):
        """
        Check whether two AABB bounding boxes intersect.
        """
        return np.all(bounds1.min(axis) <= bounds2.max(axis)) and np.all(bounds1.max(axis) >= bounds2.min(axis))

    @staticmethod
    def contains(bounds1: "Bounds", bounds2: "Bounds", *, axis: None | List[int] = None):
        """
        Check whether AABB1 fully contains AABB2.
        """
        return np.all(bounds1.min(axis) <= bounds2.min(axis)) and np.all(bounds1.max(axis) >= bounds2.max(axis))

