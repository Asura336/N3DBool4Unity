# Readme

## 1 Net3DBool

### 1 概述

包含 mesh 布尔运算所需的组件，从 https://github.com/Arakis/Net3dBool 获得。

改动包括重构部分内容使使用体验更符合 C# 的习惯，提供适应 Unity 引擎的接口，提供简单合并三角形和重计算 uv 的工具（使用类似于天空盒的方式投射 1m * 1m 的平面到网格），并汉化了部分注释。

缺点：

- 网格布尔运算本身时间复杂度较高，而且执行运算需要构造实例表示每个计算阶段的结果，有额外的内存开销。
- 不会计算网格本身的 uv，布尔运算后需要重新计算 uv。

### 2 接口

构造 ```BooleanModeller``` 实例，传入两个 ```Solid``` 实例，可计算两个物体的交集、并集和补集。补集运算不满足交换律。

扩展：

- 内置的 ```Vector3Double``` 可以和 Unity 的 ```Vector3``` 互相转化或判断相等。

- ```Solid``` 实例可以从 Unity 的 ```MeshFilter``` 构造（或者 ```Mesh``` 实例和对应的 ```Transform```），构造 ```Solid``` 需要提供 ```transform```信息。
- ```Solid``` 实例可以转化为 Unity 的 ```Mesh``` 实例。
- 扩展类 ```ObjectFaceF``` 通过顶点和三角形数组构造，通过并查集合并部分三角形，能稍微优化布尔运算的结果。
- 根据世界坐标轴从六方向投影重新计算网格 UV，需要 ```Mesh``` 网格和 ```Transform``` 信息。这种投影方式适合造型简单且材质单一的物体。

使用样例：

```c#
// 取两个 MeshFilter 的并集
public static Mesh _GetUnion(MeshFilter meshF1, MeshFilter meshF2)
{
    using (var booleanModeller =
        new BooleanModeller(meshF1.ToSolidInWCS(), meshF2.ToSolidInWCS()))
    {
        var end = booleanModeller.GetUnion();
        end.translate((-meshF1.transform.position).ToVector3Double());

        using (ObjectFaceF objF = 
            ObjectFaceF.GetInstance(end.Vertices.ToVector3Arr(), end.Triangles))
        {
            objF.MergeNeighbors();
            // 返回重计算 uv 后的 Mesh 网格
            var result = objF.ToMesh();
            result.SetUVinWCS();  // 按空间坐标投射 uv
            return result;
        }
    }
}
```

### 3 原理

网格布尔运算的第一步会使参与计算的两个网格互相干涉：

- 取参与计算的网格 A 和 B，复制网格信息。
- 用网格 B 切分网格 A——取来自网格 A 的某个三角面 a 和来自 网格 B 的某个三角面 b，若 a 和 b 相交则用 b 所在的平面分割 a。
- 用上一步的方式用网格 A 切分网格 B。
- 得到切分后的网格，这两个网格没有三角面是相交的。判断每个三角面在“A 和 B 的并集”中是“暴露在外”还是“被遮挡”，作为布尔运算的依据。
- A 和 B 的并集是所有“外露的面片”‘；A 和 B 的交集是所有“被遮挡的面片”；A 和 B 的补集（A 减去 B）是“A 中外露的面片”和“B 中被遮挡的面片”的和。