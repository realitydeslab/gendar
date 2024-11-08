using UnityEngine;

public class Effect0 : EffectBase
{


    public override void StartEffect()
    {
        base.StartEffect();

        // initialize effect
        needPushBuffer = true;
        needPushHumanStencil = false;

        // randomize color for testing
        if (player.OwnerClientId % 3 == 0) effectColor = Color.red;
        else if (player.OwnerClientId % 3 == 1) effectColor = Color.green;
        else effectColor = Color.blue;
    }

    public override void StopEffect()
    {
        base.StopEffect();
    }

    public override void UpdateVFXParameter()
    {
        base.UpdateVFXParameter();

        // set custom parameter
        // 
    }

	//float aaa(float distance, Vector2 effectRange, float effectWidth)
	//{
	//	float result = 0;
	//	// marching normally
	//	if (effectRange.x >= effectRange.y)
	//	{
	//		if (distance <= effectRange.x && distance >= effectRange.y)
	//		{
	//			if (effectRange.x == effectRange.y)
	//			{
	//				result = 1;
	//			}
	//			else
	//			{
	//				result = (distance - effectRange.y) / (effectRange.x - effectRange.y);
	//			}
	//		}
	//		else
	//		{
	//			result = 0;
	//		}
	//		//result = (distance <= effectRange.x && distance >= effectRange.y)  ? 1 : 0;
	//	}

	//	// the head has looped back while tail is still marching
	//	else
	//	{
	//		if (distance <= effectRange.x && distance >= 0)
	//		{
	//			result = (distance + effectWidth - effectRange.x) / effectWidth;
	//		}
	//		else if (distance >= effectRange.y && distance <= effectRange.y + effectWidth - effectRange.x)
	//		{
	//			result = (distance - effectRange.y) / effectWidth;
	//		}
	//		else
	//		{
	//			result = 0;
	//		}

	//		//result = (distance <= effectRange.x  && distance>=0)  || (distance >= effectRange.y && distance <= effectRange.y + effectWidth - effectRange.x)  ? 1 : 0) 
	//	}

	//	return result;
	//}
}
