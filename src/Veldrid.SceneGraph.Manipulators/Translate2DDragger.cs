

using System.Collections.Generic;
using System.Numerics;
using SixLabors.Shapes;
using Veldrid.SceneGraph.InputAdapter;
using Veldrid.SceneGraph.Manipulators.Commands;
using Veldrid.SceneGraph.PipelineStates;
using Veldrid.SceneGraph.Shaders.Standard;
using Veldrid.SceneGraph.Util;
using Veldrid.SceneGraph.Util.Shape;
using Veldrid.SceneGraph.VertexTypes;

namespace Veldrid.SceneGraph.Manipulators
{
    public interface ITranslate2DDragger : IDragger
    {
        public bool CheckForNodeInPath { get; set; }
    }
    
    public class Translate2DDragger : Base2DDragger, ITranslate2DDragger
    {
        protected Vector3 StartProjectedPoint { get; set; }

        public bool CheckForNodeInPath { get; set; } = true;
        
        private INode _leftHandleNode;
        public INode LeftHandleNode
        {
            get => _leftHandleNode;
            set
            {
                _leftHandleNode = value;
                _leftHandleNode.PipelineState = _leftHandleMaterial.CreatePipelineState();
            }
        }

        private INode _rightHandleNode;

        public INode RightHandleNode
        {
            get => _rightHandleNode;
            set
            {
                _rightHandleNode = value;
                _rightHandleNode.PipelineState = _rightHandleMaterial.CreatePipelineState();
            }
        }

        private INode _topHandleNode;

        public INode TopHandleNode
        {
            get => _topHandleNode;
            set
            {
                _topHandleNode = value;
                _topHandleNode.PipelineState = _topHandleMaterial.CreatePipelineState();
            }
        }

        private INode _bottomHandleNode;

        public INode BottomHandleNode
        {
            get => _bottomHandleNode;
            set
            {
                _bottomHandleNode = value;
                _bottomHandleNode.PipelineState = _bottomHandleMaterial.CreatePipelineState();
            }
        }

        private IPhongMaterial _leftHandleMaterial;
        private IPhongMaterial _rightHandleMaterial;
        private IPhongMaterial _topHandleMaterial;
        private IPhongMaterial _bottomHandleMaterial;
        
        private IPhongMaterial _pickedHandleMaterial;
        
        public new static ITranslate2DDragger Create()
        {
            return new Translate2DDragger(Matrix4x4.Identity);
        }
        
        protected Translate2DDragger(Matrix4x4 matrix) : base(matrix, true)
        {
            _leftHandleMaterial = CreateMaterial();
            _rightHandleMaterial = CreateMaterial();
            _topHandleMaterial = CreateMaterial();
            _bottomHandleMaterial = CreateMaterial();
        }

        public override void SetupDefaultGeometry()
        {
            var lineStart = Vector3.Multiply(Vector3.UnitZ, -0.5f);
            var lineEnd = Vector3.Multiply(Vector3.UnitZ, 0.5f);
            
            // Create a Line
            var lineGeode = Geode.Create();
            {
                var geometry = Geometry<Position3Color3>.Create();
                var vertexArray = new Position3Color3[2];
                vertexArray[0] = new Position3Color3(lineStart, Vector3.One);
                vertexArray[1] = new Position3Color3(lineEnd, Vector3.One);

                var indexArray = new uint[2];
                indexArray[0] = 0;
                indexArray[1] = 1;

                geometry.IndexData = indexArray;
                geometry.VertexData = vertexArray;
                geometry.VertexLayouts = new List<VertexLayoutDescription>()
                {
                    Position3Color3.VertexLayoutDescription
                };
                
                var pSet = DrawElements<Position3Color3>.Create(
                    geometry,
                    PrimitiveTopology.LineStrip,
                    2,
                    1,
                    0,
                    0,
                    0);
                
                geometry.PrimitiveSets.Add(pSet);

                geometry.PipelineState.ShaderSet = Position3Color3Shader.Instance.ShaderSet;

                lineGeode.AddDrawable(geometry);
            }
            
            //AddChild(lineGeode);
            var hints = TessellationHints.Create();
            hints.ColorsType = ColorsType.ColorOverall;
            //hints.NormalsType = NormalsType.PerFace;

            var pipelineState = CreateMaterial().CreatePipelineState();

            var horizontalConeGroup = Group.Create();
            
            // Create a left Cone
            {
                var geode = Geode.Create();
                var cone = Cone.Create(lineStart, 0.5f, 2f);
                cone.Rotation = QuaternionExtensions.MakeRotate(Vector3.Multiply(Vector3.UnitZ, -1.0f), Vector3.UnitZ);
                
                geode.AddDrawable(ShapeDrawable<Position3Texture2Color3Normal3>.Create(
                    cone,
                    hints,
                    new [] {new Vector3(0.0f, 1.0f, 0.0f)}));

                var autoTransform = AutoTransform.Create();
                autoTransform.Position = lineStart;
                autoTransform.PivotPoint = lineStart;
                autoTransform.AutoScaleToScreen = true;
                autoTransform.AddChild(geode);
                
                var antiSquish = AntiSquish.Create(lineStart);
                antiSquish.AddChild(autoTransform);
                
                horizontalConeGroup.AddChild(antiSquish);
                LeftHandleNode = antiSquish;
            }
            
            // Create a right Cone
            {
                var geode = Geode.Create();
                var cone = Cone.Create(lineEnd, 0.5f, 2f);
                geode.AddDrawable(ShapeDrawable<Position3Texture2Color3Normal3>.Create(
                    cone,
                    hints,
                    new [] {new Vector3(0.0f, 1.0f, 0.0f)}));

                var autoTransform = AutoTransform.Create();
                autoTransform.Position = lineEnd;
                autoTransform.PivotPoint = lineEnd;
                autoTransform.AutoScaleToScreen = true;
                autoTransform.AddChild(geode);
                
                var antiSquish = AntiSquish.Create(lineEnd);
                antiSquish.AddChild(autoTransform);
                
                horizontalConeGroup.AddChild(antiSquish);
                RightHandleNode = antiSquish;
            }
            
            var verticalConeGroup = Group.Create();
            
            // Create a top Cone
            {
                var geode = Geode.Create();
                var cone = Cone.Create(lineStart, 0.5f, 2f);
                cone.Rotation = QuaternionExtensions.MakeRotate(Vector3.Multiply(Vector3.UnitZ, -1.0f), Vector3.UnitZ);
                
                geode.AddDrawable(ShapeDrawable<Position3Texture2Color3Normal3>.Create(
                    cone,
                    hints,
                    new [] {new Vector3(0.0f, 1.0f, 0.0f)}));

                var autoTransform = AutoTransform.Create();
                autoTransform.Position = lineStart;
                autoTransform.PivotPoint = lineStart;
                autoTransform.AutoScaleToScreen = true;
                autoTransform.AddChild(geode);
                
                var antiSquish = AntiSquish.Create(lineStart);
                antiSquish.AddChild(autoTransform);
                
                verticalConeGroup.AddChild(antiSquish);
                TopHandleNode = antiSquish;
            }
            
            // Create a right Cone
            {
                var geode = Geode.Create();
                var cone = Cone.Create(lineEnd, 0.5f, 2f);
                geode.AddDrawable(ShapeDrawable<Position3Texture2Color3Normal3>.Create(
                    cone,
                    hints,
                    new [] {new Vector3(0.0f, 1.0f, 0.0f)}));

                var autoTransform = AutoTransform.Create();
                autoTransform.Position = lineEnd;
                autoTransform.PivotPoint = lineEnd;
                autoTransform.AutoScaleToScreen = true;
                autoTransform.AddChild(geode);
                
                var antiSquish = AntiSquish.Create(lineEnd);
                antiSquish.AddChild(autoTransform);
                
                verticalConeGroup.AddChild(antiSquish);
                BottomHandleNode = antiSquish;
            }

            var planeNormal = Vector3.Normalize(PlaneProjector.Plane.Normal);
            var planeRotation = QuaternionExtensions.MakeRotate(new Vector3(0.0f, 1.0f, 0.0f), planeNormal);
            var xform = MatrixTransform.Create(Matrix4x4.CreateFromQuaternion(planeRotation));
            
            // Create an arrow in the XAxis
            {
                var rot = QuaternionExtensions.MakeRotate(new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f));
                var arrow = MatrixTransform.Create(Matrix4x4.CreateFromQuaternion(rot));
                arrow.AddChild(lineGeode);
                arrow.AddChild(verticalConeGroup);
                xform.AddChild(arrow);
            }
            
            // Create an arrow in the XAxis
            {
                var arrow = Group.Create();
                arrow.AddChild(lineGeode);
                arrow.AddChild(horizontalConeGroup);
                xform.AddChild(arrow);
            }

            AddChild(xform);
        }
        
        public override bool Handle(IPointerInfo pointerInfo, IUiEventAdapter eventAdapter,
            IUiActionAdapter actionAdapter)
        {
            if (CheckForNodeInPath)
            {
                if (!pointerInfo.Contains(LeftHandleNode) && 
                    !pointerInfo.Contains(RightHandleNode) && 
                    !pointerInfo.Contains(TopHandleNode) &&
                    !pointerInfo.Contains(BottomHandleNode)) 
                    return false;
            }
            
            switch (eventAdapter.EventType)
            {
                // Pick Start
                case IUiEventAdapter.EventTypeValue.Push:
                {
                    // Get the local to world matrix for this node and set it for the projector
                    var nodePathToRoot = Util.ComputeNodePathToRoot(this);
                    var localToWorld = ComputeLocalToWorld(nodePathToRoot);
                    PlaneProjector.LocalToWorld = localToWorld;

                    if (PlaneProjector.Project(pointerInfo, out var startProjectedPoint))
                    {
                        StartProjectedPoint = startProjectedPoint;

                        if (pointerInfo.Contains(LeftHandleNode))
                        {
                            _pickedHandleMaterial = _leftHandleMaterial;
                        }
                        else if (pointerInfo.Contains(RightHandleNode))
                        {
                            _pickedHandleMaterial = _rightHandleMaterial;
                        }
                        else if (pointerInfo.Contains(TopHandleNode))
                        {
                            _pickedHandleMaterial = _topHandleMaterial;
                        }
                        else if (pointerInfo.Contains(BottomHandleNode))
                        {
                            _pickedHandleMaterial = _bottomHandleMaterial;
                        }
                        
                        // Create the motion command
                        var cmd = TranslateInPlaneCommand.Create(PlaneProjector.Plane);
                        cmd.Stage = IMotionCommand.MotionStage.Start;
                        cmd.ReferencePoint = StartProjectedPoint;
                        cmd.SetLocalToWorldAndWorldToLocal(PlaneProjector.LocalToWorld, PlaneProjector.WorldToLocal);

                        Dispatch(cmd);
                        
                        var pickColor = GetColorVector(PickColor);
                        _pickedHandleMaterial?.SetMaterial(pickColor, pickColor, Vector3.One, 1);
                        
                        actionAdapter.RequestRedraw();
                    }

                    return true;
                }
                // Pick Move
                case IUiEventAdapter.EventTypeValue.Drag:
                {
                    
                    if (PlaneProjector.Project(pointerInfo, out var projectedPoint))
                    {
                        // Generate command
                        var cmd = TranslateInPlaneCommand.Create(PlaneProjector.Plane);
                        cmd.Stage = IMotionCommand.MotionStage.Move;
                        cmd.SetLocalToWorldAndWorldToLocal(PlaneProjector.LocalToWorld, PlaneProjector.WorldToLocal);
                        cmd.Translation = projectedPoint - StartProjectedPoint;
                        cmd.ReferencePoint = StartProjectedPoint;
                        
                        Dispatch(cmd);
                        
                        actionAdapter.RequestRedraw();
                        
                    }
                    return true;
                }
                case IUiEventAdapter.EventTypeValue.Release:
                {
                    // Create the motion command
                    var cmd = TranslateInPlaneCommand.Create(PlaneProjector.Plane);
                    cmd.Stage = IMotionCommand.MotionStage.Finish;
                    cmd.ReferencePoint = StartProjectedPoint;
                    cmd.SetLocalToWorldAndWorldToLocal(PlaneProjector.LocalToWorld, PlaneProjector.WorldToLocal);
                    
                    Dispatch(cmd);
                    
                    var normalColor = GetColorVector(Color);
                    _pickedHandleMaterial?.SetMaterial(normalColor, normalColor, Vector3.One, 1);
                    
                    actionAdapter.RequestRedraw();

                    return true;
                }
                default:
                    return false;
            }
        }
    }
}