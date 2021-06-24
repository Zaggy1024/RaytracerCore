using System;
using System.Collections.Generic;
using System.Windows.Forms;

using RaytracerCore.Raytracing;
using RaytracerCore.Raytracing.Acceleration;
using RaytracerCore.Raytracing.Objects;
using RaytracerCore.Raytracing.Primitives;

namespace RaytracerCore.Inspector
{
	public partial class SceneInspector : Form
	{
		public enum DisplayCategory
		{
			Scene,
			BVH
		}

		[Flags]
		public enum DisplaySettingField
		{
			OverlayEnabled = 1,
			Category = 2,
			DisplaySelected = 4,
			Selection = 8,
			All = OverlayEnabled | Category | DisplaySelected | Selection
		}

		public class DisplaySettings
		{
			public static readonly DisplaySettings Default = new DisplaySettings()
			{
				OverlayEnabled = false,
				Category = DisplayCategory.Scene,
				DisplaySelected = false,
				Selection = null
			};

			public bool OverlayEnabled;
			public DisplayCategory Category;
			public bool DisplaySelected;
			public object Selection;
		}

		public class DisplaySettingChangedEventArgs
		{
			public DisplaySettings Settings;
			public DisplaySettingField ChangedSettings;
		}

		public event EventHandler<DisplaySettingChangedEventArgs> DisplaySettingChanged;

		private Scene _Scene;
		private object Selection;

		private List<Primitive> Primitives;
		private List<IObject> Objects;
		private Dictionary<IObject, List<Primitive>> ObjectPrimitives;

		private TreeNode LoadMoreNode = new TreeNode("Load more...");

		public SceneInspector(Scene scene)
		{
			InitializeComponent();

			ChangeScene(scene);

			Shown += (o, e) =>  SendDisplaySettingUpdate(DisplaySettingField.All);
		}

		public Scene Scene => _Scene;

		public void ChangeScene(Scene scene)
		{
			_Scene = scene;
			UpdateAll();
		}

		public DisplaySettings GetDisplaySettings()
		{
			DisplayCategory category;

			if (tabsViews.SelectedTab == tabScene)
				category = DisplayCategory.Scene;
			else
				category = DisplayCategory.BVH;

			return new DisplaySettings
			{
				OverlayEnabled = checkOverlay.Checked,
				Category = category,
				DisplaySelected = checkSelection.Checked,
				Selection = Selection
			};
		}

		private void SendDisplaySettingUpdate(DisplaySettingField changed)
		{
			DisplaySettingChanged?.Invoke(this, new DisplaySettingChangedEventArgs
			{
				Settings = GetDisplaySettings(),
				ChangedSettings = changed
			});
		}

		public void UpdateSceneSelection()
		{
			treePrimitive.BeginUpdate();
			treePrimitive.Nodes.Clear();

			if (treeScene.SelectedNode == null)
				return;

			object tag = treeScene.SelectedNode.Tag;

			if (tag is Primitive primitive)
			{
				TreeNode root = Nodifier.CreatePrimitive(primitive, "Primitive");
				root.Expand();
				treePrimitive.Nodes.Add(root);

				treePrimitive.SelectedNode = root;
			}

			treePrimitive.EndUpdate();
		}

		private void AddSceneSet()
		{
			const int batchSize = 1000;

			int count = treeScene.Nodes.Count;

			if (count == 0 || treeScene.Nodes[count - 1] == LoadMoreNode)
			{
				treeScene.BeginUpdate();

				if (count > 0)
					treeScene.Nodes.RemoveAt(count - 1);

				int firstNew = treeScene.Nodes.Count;

				int countLeft = Objects.Count + Primitives.Count - firstNew;
				TreeNode[] nodes = new TreeNode[Math.Min(batchSize, countLeft)];

				for (int i = 0; i < nodes.Length; i++)
				{
					int current = firstNew + i;

					if (current < Objects.Count)
					{
						IObject obj = Objects[current];
						List<Primitive> children = ObjectPrimitives[obj];

						TreeNode objectNode = Nodifier.Create($"Object {current} ({obj.Name}):");
						objectNode.Tag = obj;
						objectNode.Expand();

						foreach (Primitive child in children)
						{
							TreeNode childNode = Nodifier.Create($"#{child.ID} ({child.Name})");
							childNode.Tag = child;
							objectNode.Nodes.Add(childNode);
						}

						nodes[i] = objectNode;
					}
					else
					{
						Primitive primitive = Primitives[current - Objects.Count];
						TreeNode primitiveNode = Nodifier.Create($"#{primitive.ID} ({primitive.Name})");
						primitiveNode.Tag = primitive;
						nodes[i] = primitiveNode;
					}
				}

				treeScene.Nodes.AddRange(nodes);

				if (countLeft > batchSize)
				{
					treeScene.Nodes.Add(LoadMoreNode);
				}

				treeScene.EndUpdate();
			}
		}

		public void UpdateScene()
		{
			treeScene.Nodes.Clear();

			Primitives = new List<Primitive>();
			Objects = new List<IObject>();
			ObjectPrimitives = new Dictionary<IObject, List<Primitive>>();

			foreach (Primitive primitive in _Scene.Primitives)
			{
				IObject parent = primitive.Parent;

				if (checkHierarchy.Checked && parent != null)
				{
					if (!ObjectPrimitives.ContainsKey(parent))
					{
						Objects.Add(parent);
						ObjectPrimitives[parent] = new List<Primitive>();
					}

					ObjectPrimitives[parent].Add(primitive);
				}
				else
				{
					Primitives.Add(primitive);
				}
			}

			AddSceneSet();

			if (treeScene.Nodes.Count > 0)
				treeScene.SelectedNode = treeScene.Nodes[0];

			SendDisplaySettingUpdate(DisplaySettingField.All);
		}

		public void UpdateBVH()
		{
			treeBVH.BeginUpdate();
			treeBVH.Nodes.Clear();

			if (_Scene.Accelerator != null)
			{
				TreeNode root = Nodifier.CreateBVHTree(_Scene.Accelerator, "Root", _Scene);
				root.Expand();
				treeBVH.Nodes.Add(root);

				treeBVH.SelectedNode = root;
			}

			treeBVH.EndUpdate();
		}

		public void UpdateBVHSelection()
		{
			treeBVHNode.BeginUpdate();
			treeBVHNode.Nodes.Clear();

			if (treeBVH.SelectedNode == null)
				return;

			object tag = treeBVH.SelectedNode.Tag;

			if (tag is BVH<Primitive> bvhNode)
			{
				TreeNode root = Nodifier.CreateBVHInfo(bvhNode, null, _Scene);
				root.Expand();
				if (root.Nodes.ContainsKey("Object"))
					root.Nodes["Object"].Expand();
				root.Nodes["Volume"].Expand();
				treeBVHNode.Nodes.Add(root);

				treeBVHNode.SelectedNode = root;
			}

			treeBVHNode.EndUpdate();
		}

		public void UpdateAll()
		{
			UpdateScene();
			UpdateBVH();
		}

		private void checkHierarchy_CheckedChanged(object sender, EventArgs e)
		{
			UpdateScene();
		}

		private void checkOverlay_CheckedChanged(object sender, EventArgs e)
		{
			SendDisplaySettingUpdate(DisplaySettingField.OverlayEnabled);
		}

		private void checkSelection_CheckedChanged(object sender, EventArgs e)
		{
			SendDisplaySettingUpdate(DisplaySettingField.DisplaySelected);
		}

		private void tabsViews_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabsViews.SelectedTab == tabScene)
				Selection = treeScene.SelectedNode?.Tag;
			else if (tabsViews.SelectedTab == tabBVH)
				Selection = treeBVH.SelectedNode?.Tag;

			SendDisplaySettingUpdate(DisplaySettingField.Category | DisplaySettingField.Selection);
		}

		private void treeScene_AfterSelect(object sender, TreeViewEventArgs e)
		{
			UpdateSceneSelection();

			if (tabsViews.SelectedTab == tabScene)
				Selection = e.Node.Tag;
			SendDisplaySettingUpdate(DisplaySettingField.Selection);
		}

		private void treeBVH_AfterSelect(object sender, TreeViewEventArgs e)
		{
			UpdateBVHSelection();

			if (tabsViews.SelectedTab == tabBVH)
				Selection = e.Node.Tag;
			SendDisplaySettingUpdate(DisplaySettingField.Selection);
		}

		private void SceneInspector_FormClosed(object sender, FormClosedEventArgs e)
		{
			checkOverlay.Checked = false;
		}

		private void treeScene_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node == LoadMoreNode)
			{
				AddSceneSet();
			}
		}
	}
}
