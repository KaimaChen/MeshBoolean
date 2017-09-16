还在研究中。。。

# MeshBoolean
Make Boolean Operator on Mesh. In Unity.

对网格做布尔操作（合集、交集、差集）。有两种途径，一是操作网格，二是利用Shader达到看起来像那么回事的效果。

# Versions
Unity 5.4.0f3

# Scenes
## 差集
#### JustWriteZ场景
![JustWriteZ](https://github.com/KaimaChen/MeshBoolean/blob/master/Blog/JustWriteZ.PNG)
思路就是在你想制造洞的物体前写入孔洞的Z值，这样孔洞部分就镂空出来了。
在本例子中，JustWriteZ的渲染次序在2001，而地面的渲染次序在2002。
这种方法很简单，但是容易把别的东西（只要渲染次序在JustWriteZ之后）都镂空，而且这种镂空是中穿物体的，而不能做到表面剜出孔的效果。

## 合集
#### JustWriteBackZ场景
