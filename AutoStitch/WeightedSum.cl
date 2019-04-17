__kernel void weighted_sum(
			__global double *data,
			__global int *offsets_x,
			__global int *offsets_y,
			__global double *weights,
			__global int *dims,
			__global double *output)
{
    int2 coord = (int2)(get_global_id(1),get_global_id(0));
    double v=0,w=0;
    for(int i=0;i<dims[2];i++){
		int x=coord.x+offsets_x[i];
		int y=coord.y+offsets_y[i];
		if(0<=x&&x<dims[1]&&0<=y&&y<dims[0]){
			v+=data[y*dims[1]+x]*weights[i];
			w+=weights[i];
		}
	}
    output[coord.y*dims[1]+coord.x]=v/w;
}