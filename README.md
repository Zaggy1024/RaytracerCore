# RaytracerCore
A simple path tracer written in Visual C# using .NET Core.

You may notice that RaytracerCore is a misnomer, this is because the project began as a ray tracer using phong reflection and multiple bounces per initial hit.

## Implementation
The implemented primitives are:
* Triangles
* Spheres
* Planes

Materials are defined per-primitive through only the following values (no texture mapping at the moment):
* *Emission:* The amount of light emitted by the surface. Used to create lights, most materials using this shouldn't define any reflective colors.
* *Diffuse:* The diffuse bounce color and intensity.
* *Specular:* The color and intensity of mirror reflections. There is no differentiation between mirror reflection and light shine in phong reflection.
* *Transmission:* The color and intensity of the light transmitted through the surface. Any ray entering a primitive on the outside should also exit through the same or another primitive on the inside.
* *Refractive index:* Defined along with the transmission color, this will determine the distortion of rays entering and exiting an object.
* *Shininess:* The amount that the surface normal will be randomized as rays reflecting or refracting exit it.

Triangle/sphere intersection and matrix operations have separate implementations using scalar math or SIMD for faster calculation.

## Features
There is a path trace inspector included to aid in debugging any issues with the implementation of the path tracer or its intersection algorithms.

## Screenshots
![The main interface](/Screenshots/app.png)
![Cornell box](/Screenshots/bounce-with-lens.png)
![Scene with die](/Screenshots/die.png)

## To do
* Implement a BVH to accelerate ray tracing.
* Improve the materials system (shared materials, textures)
* Determine the ratio of reflected to transmitted rays using [Fresnel equations](https://en.wikipedia.org/wiki/Fresnel_equations).