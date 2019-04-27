__kernel void weighted_sum(
			__global double *source_points,
			__global double *target_points,
			__global int *dims,
			__global int *output)
{
    int id = get_global_id(0);
	int ans=-1;
	double first_min=1e300,second_min=1e300;
	for(int i=0;i<dims[2];i++){
		double dif=0;
		for(int j=0;j<dims[0];j++){
			double v=source_points[id*dims[0]+j]-target_points[i*dims[0]+j];
			dif+=v*v;
		}
		if(dif<first_min){
			second_min=first_min;
			first_min=dif;
			ans=i;
		}else if(dif<second_min){
			second_min=dif;
		}
	}
	if(first_min/second_min<0.8*0.8)output[id]=ans;
	else output[id]=-1;
}