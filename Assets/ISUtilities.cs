using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public static class ISUtilities
{
    public enum POTENTIAL_FUNCTION
    {
        QUARTIC_METABALL
    }
    
    static Func<float, (float, float)> GetPotGradFunction(POTENTIAL_FUNCTION func){ 
        switch(func){
            case POTENTIAL_FUNCTION.QUARTIC_METABALL:
                return EvaluateQuarticMetaballPotentialAndGradient;
        }
        return null;
    }

    public static (float, float)EvaluatePotentialAndGradient(SkeletonData surface, Vector3 point){
        float r2 = ((surface.position - point)/surface.scale).sqrMagnitude;
        return GetPotGradFunction(surface.potentialFunction)(r2);
    }

    static (float, float) EvaluateQuarticMetaballPotentialAndGradient(float r2){
        if(r2 > 0.49) return (0, 0); 
        float r = Mathf.Sqrt(r2);
        return (r2*(r2 - 1) + 0.25f, r*(4*r2 - 2));
    }




    public static float DichotomicSearch(SkeletonData surf){
        return DichotomicSearch(GetPotGradFunction(surf.potentialFunction), surf.isoValue);
    }
    
    static float DichotomicSearch(Func<float, (float, float)> potgrad, float isoValue, float eps = 0.001f){
        //looks for the single point where pot(r) = isoValue for a potential function maxed at 0 and decreasing 
        float r2min = 0;
        float r2max = 1;

        Func<float, float> pot = x => potgrad(x).Item1;

        float potrmin = pot(r2min);
        float potrmax = pot(r2max);

        int itCounter = 0; 
        //look for an upper bound
        while(potrmax > isoValue){
            r2max *= 2;
            potrmax = pot(r2max);
            itCounter ++; 
            if(itCounter > 1000) return -1;
        }

        //dichotomy 
        while(potrmin - potrmax > eps){
            float r = (r2min + r2max)/2;
            float potr = pot(r);
            if(potr < isoValue){
                r2max = r;
                potrmax = potr;
            }
            else{
                r2min = r;
                potrmin = potr;
            } 
            itCounter++;
            if(itCounter > 1000) return -2;
        }
        return Mathf.Sqrt(r2min);
    }
}
