Shader "Kaima/MeshBoolean/JustWriteBackZ"
{
	SubShader
	{
		Tags { "Queue"="Geometry+1" } //需要在你想要开洞的物体前

		Pass //目的只是写入背面Z
		{
			Cull Front
			//下面两句的功能是一样的
			Blend Zero One 
			//ColorMask 0 
		}
	}
}
