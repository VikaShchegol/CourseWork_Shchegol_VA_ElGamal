using System;

namespace CourseWork_Shchegol
{
    public static class VariantConfig
    {
        public static int N = 8;    
        public static int S = 3;     
        public static double T = 10; 
        public static int R = 10;   
        public static string Cipher = "ElGamal";
        public static double a = 4.0; 

        public static double F(double x)
        {
            return Math.Log10(a * x);
        }
    }
}
