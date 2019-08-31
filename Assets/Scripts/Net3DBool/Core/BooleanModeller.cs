/*
The MIT License (MIT)

Copyright (c) 2014 Sebastian Loncar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

See:
D. H. Laidlaw, W. B. Trumbore, and J. F. Hughes.
"Constructive Solid Geometry for Polyhedral Objects"
SIGGRAPH Proceedings, 1986, p.161.

original author: Danilo Balby Silva Castanheira (danbalby@yahoo.com)

Ported from Java to C# by Sebastian Loncar, Web: http://www.loncar.de
Project: https://github.com/Arakis/Net3dBool

Optimized and refactored by: Lars Brubaker (larsbrubaker@matterhackers.com)
Project: https://github.com/MatterHackers/agg-sharp (an included library)
*/

using System.Collections.Generic;

namespace Net3dBool
{
    /// <summary>
    /// 为<see cref="Solid"/>实例应用集合运算的类；
    /// 在构造函数中提交两个<see cref="Solid"/>实例；
    /// 每个布尔运算的方法都会返回一个新的<see cref="Solid"/>实例存放运算结果；
    /// </summary>
    public class BooleanModeller
    {
        /** solid where boolean operations will be applied */
        private Object3D object1;
        private Object3D object2;

        //--------------------------------CONSTRUCTORS----------------------------------//

        /// <summary>
        /// 传入两个物体并初步划分表面，不会影响原物体
        /// </summary>
        /// <param name="solid1">布尔运算的第一个参数</param>
        /// <param name="solid2">布尔运算的第二个参数</param>
        public BooleanModeller(Solid solid1, Solid solid2)
        {
            //representation to apply boolean operations
            object1 = new Object3D(solid1);
            object2 = new Object3D(solid2);

            //split the faces so that none of them intercepts each other
            object1.SplitFaces(object2);
            object2.SplitFaces(object1);

            //classify faces as being inside or outside the other solid
            object1.ClassifyFaces(object2);
            object2.ClassifyFaces(object1);
        }

        private BooleanModeller() { }

        //----------------------------------OVERRIDES-----------------------------------//

        /**
        * Clones the BooleanModeller object
        *
        * @return cloned BooleanModeller object
        */

        public BooleanModeller Clone()
        {
            BooleanModeller clone = new BooleanModeller
            {
                object1 = object1.Clone(),
                object2 = object2.Clone()
            };
            return clone;
        }

        //-------------------------------BOOLEAN_OPERATIONS-----------------------------//

        /// <summary>
        /// 取补集
        /// </summary>
        /// <returns></returns>
        public Solid GetDifference()
        {
            object2.InvertInsideFaces();
            Solid result = ComposeSolid(Status.OUTSIDE, Status.OPPOSITE, Status.INSIDE);
            object2.InvertInsideFaces();  // 重置以取消副作用
            return result;
        }

        /// <summary>
        /// 取交集
        /// </summary>
        /// <returns></returns>
        public Solid GetIntersection()
        {
            return ComposeSolid(Status.INSIDE, Status.SAME, Status.INSIDE);
        }

        /// <summary>
        /// 取并集
        /// </summary>
        /// <returns></returns>
        public Solid GetUnion()
        {
            return ComposeSolid(Status.OUTSIDE, Status.SAME, Status.OUTSIDE);
        }

        //--------------------------PRIVATES--------------------------------------------//

        /// <summary>
        /// 基于面的状态和作为参数的物体生成新物体。
        /// 状态：<see cref="Status.INSIDE"/>, <see cref="Status.OUTSIDE"/>, 
        /// <see cref="Status.SAME"/>, <see cref="Status.OPPOSITE"/>
        /// </summary>
        /// <param name="faceStatus1">筛选第一个物体上的面</param>
        /// <param name="faceStatus2">筛选第一个物体上的面
        /// (与第二个物体共面时选取此状态的面)</param>
        /// <param name="faceStatus3">筛选第二个物体上的面</param>
        /// <returns></returns>
        private Solid ComposeSolid(Status faceStatus1, Status faceStatus2, Status faceStatus3)
        {
            var vertices = new List<Vertex>();
            var indices = new List<int>();

            // group the elements of the two solids whose faces fit with the desired status
            GroupObjectComponents(object1, vertices, indices, faceStatus1, faceStatus2);
            GroupObjectComponents(object2, vertices, indices, faceStatus3, faceStatus3);

            Vector3Double[] verticesArray = new Vector3Double[vertices.Count];
            for (int i = 0; i < vertices.Count; i++) { verticesArray[i] = vertices[i].Position; }

            //returns the solid containing the grouped elements
            return new Solid(verticesArray, indices.ToArray());
        }

        /// <summary>
        /// 按特定条件选取物件中适合的三角面填充入容器
        /// </summary>
        /// <param name="obj">选取面的物体</param>
        /// <param name="vertices">存放选取的顶点</param>
        /// <param name="indices">存放选取的三角形序号</param>
        /// <param name="faceStatus1">第一个筛选条件</param>
        /// <param name="faceStatus2">第二个筛选条件</param>
        private void GroupObjectComponents(Object3D obj, List<Vertex> vertices, List<int> indices, Status faceStatus1, Status faceStatus2)
        {
            for (int i = 0; i < obj.GetNumFaces(); i++)
            {
                var face = obj.GetFace(i);
                if (face.Status == faceStatus1 || face.Status == faceStatus2)  // if the face status fits with the desired status...
                {
                    Vertex[] faceVerts = { face.v1, face.v2, face.v3 };  // adds the face elements into the arrays
                    for (int j = 0; j < faceVerts.Length; j++)
                    {
                        if (vertices.Contains(faceVerts[j]))
                        {
                            indices.Add(vertices.IndexOf(faceVerts[j]));
                        }
                        else
                        {
                            indices.Add(vertices.Count);
                            vertices.Add(faceVerts[j]);
                        }
                    }
                }
            }
        }
    }
}