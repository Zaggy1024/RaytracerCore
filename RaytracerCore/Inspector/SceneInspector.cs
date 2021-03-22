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
			treePrimitive.SuspendLayout();
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

			treePrimitive.ResumeLayout();
		}

		public void UpdateScene()
		{
			treeScene.SuspendLayout();
			treeScene.Nodes.Clear();

			IList<Primitive> primitives = _Scene.Primitives;

			// TODO: Fill on demand so that large numbers of primitives don't freeze the app.
			if (primitives.Count < 1000)
			{
				Dictionary<IObject, TreeNode> objects = null;
				List<TreeNode> nodesToAdd = new List<TreeNode>();

				// Create dictionary of objects to their nodes so that we can create a simple hierarchy.
				if (checkHierarchy.Checked)
				{
					objects = new Dictionary<IObject, TreeNode>();
					int objectI = 0;

					foreach (Primitive primitive in primitives)
					{
						IObject parent = primitive.Parent;

						if (parent != null && !objects.ContainsKey(parent))
						{
							TreeNode objectNode = Nodifier.Create($"Object {objectI} ({parent.Name}):");
							objectNode.Tag = parent;
							objects.Add(parent, objectNode);
							objectNode.Expand();

							nodesToAdd.Add(objectNode);
							objectI++;
						}
					}
				}

				// Add each primitive to its parent object's node, or the root.
				foreach (Primitive primitive in primitives)
				{
					TreeNode node = Nodifier.Create($"#{primitive.ID} ({primitive.Name})");
					node.Tag = primitive;

					if (primitive.Parent != null && objects != null)
						objects[primitive.Parent].Nodes.Add(node);
					else
						nodesToAdd.Add(node);
				}

				treeScene.Nodes.AddRange(nodesToAdd.ToArray());
			}

			treeScene.ResumeLayout();

			if (treeScene.Nodes.Count > 0)
				treeScene.SelectedNode = treeScene.Nodes[0];
		}

		public void UpdateBVHSelection()
		{
			treeBVHNode.SuspendLayout();
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

			treeBVHNode.ResumeLayout();
		}

		public void UpdateBVH()
		{
			treeBVH.SuspendLayout();
			treeBVH.Nodes.Clear();

			if (_Scene.Accelerator != null)
			{
				TreeNode root = Nodifier.CreateBVHTree(_Scene.Accelerator, "Root", _Scene);
				root.Expand();
				treeBVH.Nodes.Add(root);

				treeBVH.SelectedNode = root;
			}

			treeBVH.ResumeLayout();
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
	}
}
