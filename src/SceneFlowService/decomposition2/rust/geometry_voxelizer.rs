use std::cmp::min;
use std::io::{self, Read, Write};
use std::sync::Arc;
use std::thread;

#[derive(Clone, Copy)]
struct Triangle {
    p: [[f32; 3]; 3],
}

#[derive(Clone, Copy)]
struct Grid {
    shape: [u32; 3],
    origin: [f32; 3],
    voxel_size: f32,
}

fn read_u32<R: Read>(reader: &mut R) -> io::Result<u32> {
    let mut buf = [0u8; 4];
    reader.read_exact(&mut buf)?;
    Ok(u32::from_le_bytes(buf))
}

fn read_u64<R: Read>(reader: &mut R) -> io::Result<u64> {
    let mut buf = [0u8; 8];
    reader.read_exact(&mut buf)?;
    Ok(u64::from_le_bytes(buf))
}

fn read_f32<R: Read>(reader: &mut R) -> io::Result<f32> {
    let mut buf = [0u8; 4];
    reader.read_exact(&mut buf)?;
    Ok(f32::from_le_bytes(buf))
}

fn edge_len(a: [f32; 3], b: [f32; 3]) -> f32 {
    let dx = a[0] - b[0];
    let dy = a[1] - b[1];
    let dz = a[2] - b[2];
    (dx * dx + dy * dy + dz * dz).sqrt()
}

fn push_index(indexes: &mut Vec<u32>, grid: Grid, p: [f32; 3]) {
    let x = ((p[0] - grid.origin[0]) / grid.voxel_size).floor() as i32;
    let y = ((p[1] - grid.origin[1]) / grid.voxel_size).floor() as i32;
    let z = ((p[2] - grid.origin[2]) / grid.voxel_size).floor() as i32;
    if x < 0 || y < 0 || z < 0 {
        return;
    }
    let x = x as u32;
    let y = y as u32;
    let z = z as u32;
    if x >= grid.shape[0] || y >= grid.shape[1] || z >= grid.shape[2] {
        return;
    }
    indexes.push((x * grid.shape[1] + y) * grid.shape[2] + z);
}

fn voxelize_triangle(indexes: &mut Vec<u32>, grid: Grid, tri: Triangle) {
    let e0 = edge_len(tri.p[0], tri.p[1]);
    let e1 = edge_len(tri.p[1], tri.p[2]);
    let e2 = edge_len(tri.p[2], tri.p[0]);
    let max_edge = e0.max(e1).max(e2);
    let steps = ((max_edge / grid.voxel_size).ceil() as u32).max(1);

    push_index(indexes, grid, tri.p[0]);
    push_index(indexes, grid, tri.p[1]);
    push_index(indexes, grid, tri.p[2]);
    push_index(
        indexes,
        grid,
        [
            (tri.p[0][0] + tri.p[1][0] + tri.p[2][0]) / 3.0,
            (tri.p[0][1] + tri.p[1][1] + tri.p[2][1]) / 3.0,
            (tri.p[0][2] + tri.p[1][2] + tri.p[2][2]) / 3.0,
        ],
    );

    let inv_steps = 1.0 / steps as f32;
    for i in 0..=steps {
        for j in 0..=(steps - i) {
            let a = i as f32 * inv_steps;
            let b = j as f32 * inv_steps;
            let c = 1.0 - a - b;
            push_index(
                indexes,
                grid,
                [
                    tri.p[0][0] * a + tri.p[1][0] * b + tri.p[2][0] * c,
                    tri.p[0][1] * a + tri.p[1][1] * b + tri.p[2][1] * c,
                    tri.p[0][2] * a + tri.p[1][2] * b + tri.p[2][2] * c,
                ],
            );
        }
    }
}

fn process_chunk(triangles: Arc<Vec<Triangle>>, grid: Grid, start: usize, end: usize) -> Vec<u32> {
    let mut indexes = Vec::new();
    for tri in &triangles[start..end] {
        voxelize_triangle(&mut indexes, grid, *tri);
    }
    indexes
}

fn main() -> io::Result<()> {
    let mut stdin = io::stdin().lock();
    let mut magic = [0u8; 4];
    stdin.read_exact(&mut magic)?;
    if &magic != b"SFV1" {
        return Err(io::Error::new(io::ErrorKind::InvalidData, "invalid magic"));
    }

    let grid = Grid {
        shape: [read_u32(&mut stdin)?, read_u32(&mut stdin)?, read_u32(&mut stdin)?],
        origin: [read_f32(&mut stdin)?, read_f32(&mut stdin)?, read_f32(&mut stdin)?],
        voxel_size: read_f32(&mut stdin)?,
    };
    let triangle_count = read_u64(&mut stdin)? as usize;
    let mut triangles = Vec::with_capacity(triangle_count);
    for _ in 0..triangle_count {
        let mut values = [0.0f32; 9];
        for value in &mut values {
            *value = read_f32(&mut stdin)?;
        }
        triangles.push(Triangle {
            p: [
                [values[0], values[1], values[2]],
                [values[3], values[4], values[5]],
                [values[6], values[7], values[8]],
            ],
        });
    }

    let threads = thread::available_parallelism().map_or(1, |n| n.get()).max(1);
    let chunk = (triangles.len() + threads - 1) / threads;
    let triangles = Arc::new(triangles);
    let mut handles = Vec::new();
    for thread_index in 0..threads {
        let start = thread_index * chunk;
        if start >= triangles.len() {
            break;
        }
        let end = min(start + chunk, triangles.len());
        let triangles_ref = Arc::clone(&triangles);
        handles.push(thread::spawn(move || process_chunk(triangles_ref, grid, start, end)));
    }

    let mut indexes = Vec::new();
    for handle in handles {
        let mut chunk_indexes = handle
            .join()
            .map_err(|_| io::Error::new(io::ErrorKind::Other, "worker thread panicked"))?;
        indexes.append(&mut chunk_indexes);
    }
    indexes.sort_unstable();
    indexes.dedup();

    let mut stdout = io::stdout().lock();
    stdout.write_all(&(indexes.len() as u64).to_le_bytes())?;
    for index in indexes {
        stdout.write_all(&index.to_le_bytes())?;
    }
    Ok(())
}
