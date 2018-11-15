﻿//
// Copyright (c) 2018 Sean Spicer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace Veldrid.SceneGraph
{
    public class CollectParentPaths : NodeVisitor
    {
        private INode _haltTraversalAtNode;
        private List<LinkedList<INode>> _nodePaths;
        
        public CollectParentPaths(INode haltTraversalAtNode = null) :
            base(VisitorType.NodeVisitor, TraversalModeType.TraverseParents)
        {
            _haltTraversalAtNode = haltTraversalAtNode;
            _nodePaths = new List<LinkedList<INode>>();
        }

        public override void Apply(INode node)
        {
            if (node.NumParents == 0 || node == _haltTraversalAtNode)
            {
                _nodePaths.Add(NodePath);
            }
            else
            {
                Traverse(node);
            }
        }
    }
   
    public abstract class Node : Object, INode
    {
        // Public Fields
        public Guid Id { get; private set; }
        public uint NodeMask { get; set; } = 0xffffffff;

        public string NameString { get; set; } = string.Empty;
        
        public int NumParents => _parents.Count;

        public bool CullingActive { get; set; } = true;
        public int NumChildrenWithCullingDisabled { get; set; } = 0;

        public bool IsCullingActive => NumChildrenWithCullingDisabled == 0 && CullingActive && GetBound().Valid();
            
        public StateSet StateSet
        {
            get => _stateSet;
            set
            {
                if (value == _stateSet) return;
                
                var deltaUpdate = 0;
                var deltaEvent = 0;
                
                if (null != _stateSet)
                {
                    _stateSet.RemoveParent(this);
                    if (_stateSet.RequiresUpdateTraversal()) --deltaUpdate;
                    if (_stateSet.RequiresEventTraversal()) --deltaEvent;
                }
                
                if (deltaUpdate!=0)
                {
                    SetNumChildrenRequiringUpdateTraversal(GetNumChildrenRequiringUpdateTraversal()+deltaUpdate);
                }

                if (deltaEvent!=0)
                {
                    SetNumChildrenRequiringEventTraversal(GetNumChildrenRequiringEventTraversal()+deltaEvent);
                }
            } 
        }

        private PipelineState _pipelineState = null;
        public PipelineState PipelineState
        {
            get => _pipelineState ?? (_pipelineState = new PipelineState());
            set => _pipelineState = value;
        }
        
        public bool HasPipelineState
        {
            get => null != _pipelineState;
        }

        public int GetNumChildrenRequiringEventTraversal()
        {
            throw new NotImplementedException();
        }

        public int GetNumChildrenRequiringUpdateTraversal()
        {
            throw new NotImplementedException();
        }

        public void SetNumChildrenRequiringEventTraversal(int i)
        {
            throw new NotImplementedException();
        }

        public void SetNumChildrenRequiringUpdateTraversal(int i)
        {
            throw new NotImplementedException();
        }

        // Protected/Private fields

        protected StateSet _stateSet = null;
        private List<IGroup> _parents;
        protected bool _boundingSphereComputed = false;
        protected BoundingSphere _boundingSphere = new BoundingSphere();

       
        private BoundingSphere _initialBound = new BoundingSphere();
        public BoundingSphere InitialBound
        {
            get { return _initialBound; }
            set
            {
                _initialBound = value;
                DirtyBound();
            }
        } 

        public event Func<Node, BoundingSphere> ComputeBoundCallback;
        
        protected Node()
        {
            Id = Guid.NewGuid();

            _parents = new List<IGroup>();
        }

        public StateSet GetOrCreateStateSet()
        {
            if (null == _stateSet)
            {
                _stateSet = new StateSet();
            }
            
            return _stateSet;
            
            
        }

        public void AddParent(IGroup parent)
        {
            _parents.Add(parent);
        }

        public void RemoveParent(IGroup parent)
        {
            _parents.RemoveAll(x => x.Id == parent.Id);
        }
 
        /// <summary>
        /// Mark this node's bounding sphere dirty.  Forcing it to be computed on the next call
        /// to GetBound();
        /// </summary>
        public void DirtyBound()
        {
            if (!_boundingSphereComputed) return;
            
            _boundingSphereComputed = false;
                
            foreach (var parent in _parents)
            {
                parent.DirtyBound();
            }
        }

        /// <summary>
        /// Get the bounding sphere for this node.
        /// </summary>
        /// <returns></returns>
        public BoundingSphere GetBound()
        {
            if (_boundingSphereComputed) return _boundingSphere;
            
            _boundingSphere = _initialBound;

            _boundingSphere.ExpandBy(null != ComputeBoundCallback ? ComputeBoundCallback(this) : ComputeBound());

            _boundingSphereComputed = true;

            return _boundingSphere;
        }

        /// <summary>
        /// Compute the bounding sphere of this geometry
        /// </summary>
        /// <returns></returns>
        public virtual BoundingSphere ComputeBound()
        {
            return new BoundingSphere();
        }
        
        public virtual void Accept(NodeVisitor nv)
        {
            if (nv.ValidNodeMask(this))
            {
                nv.PushOntoNodePath(this);
                nv.Apply(this);
                nv.PopFromNodePath(this);
            };
        }

        public virtual void Ascend(NodeVisitor nv)
        {
            foreach (var parent in _parents)
            {
                parent.Accept(nv);
            }
        }

        // Traverse downward - call children's accept method with Node Visitor
        public virtual void Traverse(NodeVisitor nv)
        {
            // Do nothing by default
        }
    }
}