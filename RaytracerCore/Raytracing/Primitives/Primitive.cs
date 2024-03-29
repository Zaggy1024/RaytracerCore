﻿using System;
using System.Collections.Generic;

using RaytracerCore.Raytracing.Objects;
using RaytracerCore.Raytracing.Acceleration;
using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Primitives
{
	/// <summary>
	/// Contains methods necessary to ray trace any primitive representation,
	/// and the material for the primitive.
	/// </summary>
	public abstract class Primitive : IBoundedObject
	{
		public IObject Parent = null;

		public int ID = -1;

		private DoubleColor _Specular;
		private DoubleColor _Refraction;

		public Primitive()
		{
			// Define material properties
			// TODO: Make material representation to share between primitives, possibly with texture mapping
			Emission = DoubleColor.Black;
			Diffuse = DoubleColor.Black;
			_Specular = DoubleColor.Black;
			_Refraction = DoubleColor.Black;
			Shininess = 100;
		}

		/// <summary>
		/// Get an array of possible intersections and their properties, with the closest hit first.
		/// </summary>
		/// <param name="ray">The ray to intersect along.</param>
		internal abstract Hit[] DoRayTrace(Ray ray);

		/// <summary>
		/// Find the closest hit on this primitive that is forward along the <paramref name="ray"/> provided.
		/// </summary>
		/// <param name="ray">The ray to trace along.</param>
		/// <param name="skip">A previous hit to ignore in cases where we may ray trace the same primitive again on a bouncing. Provide default(Hit) to never ignore.</param>
		/// <returns>The first hit along the <paramref name="ray"/>.</returns>
		public Hit RayTrace(Ray ray, Hit skip)
		{
			Hit[] hits = DoRayTrace(ray);
			Hit hit = default;

			if (hits != default)
			{
				for (int i = 0; i < hits.Length; i++)
				{
					Hit curHit = hits[i];

					if (curHit == default)
						continue;

					if (Invert)
						curHit = curHit.Inverted();

					if (curHit.Inside && !TwoSided)
						continue;

					if (!Util.RayHitMatches(ray, curHit, skip))
					{
						hit = curHit;
						break;
					}
				}
			}

			return hit;
		}

		/// <summary>
		/// Apply a transformation to the primitive on top of any previous transformations.
		/// </summary>
		/// <param name="objectMatrix">The world-to-object transformation matrix.</param>
		/// <param name="worldMatrix">The object-to-world transformation matrix.</param>
		public abstract void Transform(Mat4x4D objectMatrix, Mat4x4D worldMatrix);

		/// <summary>
		/// Whether the primitive will discount rays originating from behind the "outward" normal (however that is defined by the primitive).
		/// </summary>
		public virtual bool TwoSided { get; set; }
		/// <summary>
		/// Whether the intersections will be inverted, affecting the normals and whether the intersection is considered to be "inside" the primitive.
		/// </summary>
		public virtual bool Invert { get; set; }

		/// <summary>
		/// The amount and color of light to be emitted from the surface of this primitive.
		/// </summary>
		public DoubleColor Emission { get; set; }

		/// <summary>
		/// The amount and color of light to be bounced in all directions off the surface.
		/// </summary>
		public DoubleColor Diffuse { get; set; }

		/// <summary>
		/// The smoothness of the surface of the object. Affects specular and refraction.
		/// 
		/// <para>A value close to but greater than 0 will be rough,
		/// and infinity will be perfectly smooth.</para>
		/// </summary>
		public double Shininess { get; set; }

		public bool IsReflective => Shininess > 0;

		/// <summary>
		/// The amount and color of light to be bounced across the normal of the surface.
		/// </summary>
		public DoubleColor Specular
		{
			get => IsReflective ? _Specular : DoubleColor.Black;
			set => _Specular = value;
		}

		/// <summary>
		/// The amount and color of light to be transmitted through the surface.
		/// </summary>
		public DoubleColor Refraction
		{
			get => IsReflective ? _Refraction : DoubleColor.Black;
			set => _Refraction = value;
		}

		/// <summary>The refractive index of the surface.
		/// When using this value, any ray entering the primitive should enter an inside=false hit and exit an inside=true hit.</summary>
		public double RefractiveIndex { get; set; }

		// IBoundedObject methods
		public abstract Vec4D GetCenter();
		public abstract double GetMaxCenterDistance(Vec4D direction);

		protected static void AddIfNonZero(string name, DoubleColor color, List<(string name, object value)> properties)
		{
			if (color != DoubleColor.Black && color != DoubleColor.Placeholder)
				properties.Add((name, color));
		}

		protected static void AddIfNonZero(string name, double value, List<(string name, object value)> properties)
		{
			if (value != 0)
				properties.Add((name, value));
		}

		public virtual List<(string name, object value)> Properties
		{
			get
			{
				var properties = new List<(string name, object value)>();
				properties.Add(("Two-sided", TwoSided));
				AddIfNonZero("Emission", Emission, properties);
				AddIfNonZero("Diffuse", Diffuse, properties);
				properties.Add(("Shininess", Shininess));

				if (IsReflective)
				{
					AddIfNonZero("Specular", Specular, properties);
					AddIfNonZero("Refraction", Refraction, properties);
				}

				properties.Add(("Refractive Index", RefractiveIndex));
				return properties;
			}
		}

		public abstract string Name { get; }
	}
}
