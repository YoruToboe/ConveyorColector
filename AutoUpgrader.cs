using UnityEngine;
using System.Collections;

public class AutoUpgrader: MachineEntity, PowerConsumerInterface 
{

	bool mbLinkedToGO;
	
	Animation mAnimation;

	Vector3 mForwards;

	public bool mbNotFacingConveyor;

	
	// **************************************************************************************************************************d**********************
	public AutoUpgrader(Segment segment, long x, long y, long z, ushort cube, byte flags, ushort lValue, bool loadFromDisk) :
		base(eSegmentEntity.AutoUpgrader, SpawnableObjectEnum.AutoUpgrader, x, y, z, cube, flags, lValue, Vector3.zero, segment)
	{

		mbNeedsLowFrequencyUpdate = true;
		mbNeedsUnityUpdate = true;
		
		mForwards = SegmentCustomRenderer.GetRotationQuaternion(flags) * Vector3.forward;
		mForwards.Normalize();

		maAttachedHoppers = new StorageMachineInterface[6];//yay for references
	}
	
	public override void OnUpdateRotation (byte newFlags)
	{
		base.OnUpdateRotation(newFlags);
	
		mForwards = SegmentCustomRenderer.GetRotationQuaternion(newFlags) * Vector3.forward;
		mForwards.Normalize();
		mbRequestReset = true;
	}	
	
	// ************************************************************************************************************************************************
	public override void DropGameObject ()
	{
		base.DropGameObject ();
		mbLinkedToGO = false;
	}
	// ************************************************************************************************************************************************
	public override void UnityUpdate()
	{
		if (!mbLinkedToGO)
		{
			if (mWrapper == null || mWrapper.mbHasGameObject == false) 
			{
				return;
			}
			else
			{
				if (mWrapper.mGameObjectList == null) Debug.LogError("RA missing game object #0?");
				if (mWrapper.mGameObjectList[0].gameObject == null) Debug.LogError("RA missing game object #0 (GO)?");
				
				mAnimation = mWrapper.mGameObjectList[0].GetComponentInChildren<Animation>();
			}
		}
	}
	// ************************************************************************************************************************************************
	float mrScanDelay;
	ushort mLastCube;

	long mnSearchX;
	long mnSearchY;
	long mnSearchZ;

	int mnSearchDirX;
	int mnSearchDirY;
	int mnSearchDirZ;

	public int mnSearchDistance;
	public int mnConversions;

	public bool mbUpgradeComplete;

	public float mrPowerPerUpgrade = 128;
	public bool mbRequestReset;
	public void Reset()
	{
		mbRequestReset = true;
	}
	// ************************************************************************************************************************************************
	public override void LowFrequencyUpdate()
	{
		if (mbUpgradeComplete && !mbRequestReset) return;
		mrScanDelay-=LowFrequencyThread.mrPreviousUpdateTimeStep;

		if (mrScanDelay > 0) return;

		if (mrCurrentPower < mrPowerPerUpgrade) return;
		//1) do we have power?

		//2) search for a conveyor of type eConveyor(0), type eBasicConveyor(11) or type Pipe(2)
		//3) If we locate that type, look for the upgrade; build over the existing type (and recall?) 
		//4) If the type is not upgradeable, then continue to iterate until we find something that is, or something that is the wrong type

		if (mnSearchX == 0 || mnSearchY == 0 || mnSearchZ == 0 || mbRequestReset)
		{
			mbRequestReset = false;
			mnSearchDirX = (int)mForwards.x;
			mnSearchDirY = (int)mForwards.y;
			mnSearchDirZ = (int)mForwards.z;

			//start at us! ;-)
			mnSearchX = mnX + mnSearchDirX;
			mnSearchY = mnY + mnSearchDirY;
			mnSearchZ = mnZ + mnSearchDirZ;

			mnConversions = 0;
			mnSearchDistance = 0;
			mbNotFacingConveyor = false;
			mbUpgradeComplete = false;
		}

		if (GetCube(mnSearchX,mnSearchY,mnSearchZ))
		{



			//attempt to upgrade; if we fail, move on; player can always request a reset if we missed anything
			//This... should... also mean that, if we decide to re-collect the resources, that we'll upgrade low to med, and med to high
			//Unsure! ;-)


			if (mLastCube == eCubeTypes.Conveyor)
			{
				if (meGetType == ConveyorEntity.eConveyorType.eBasicConveyor || meGetType == ConveyorEntity.eConveyorType.eConveyor)
				{
					UpdateAttachedHoppers(false);

					if (mnNumValidAttachedHoppers == 0) return;//come back next go, hopefully the user has worked out how to use us. ¬.¬

					ConveyorEntity.eConveyorType lBuildType = ConveyorEntity.eConveyorType.eNumConveyorTypes;

					ushort lConveyor = (ushort)ConveyorEntity.eConveyorType.eConveyor;
					ushort lPipe = (ushort)ConveyorEntity.eConveyorType.eTransportPipe;

					for (int i=0;i<mnNumValidAttachedHoppers;i++)
					{
						StorageMachineInterface lHopper = maAttachedHoppers[i];

                        if (lHopper == null) continue;//?! breks when rotated?

						//attempt to upgrade to pipes first
						if (lHopper.TryExtractCubes(this, eCubeTypes.Conveyor, lPipe, 1))
						{
							lBuildType = ConveyorEntity.eConveyorType.eTransportPipe;
							break;							
						}


//						if (lHopper.RemoveInventoryCube(eCubeTypes.Conveyor,lPipe,1) == 1) 	
//						{
//							lBuildType = ConveyorEntity.eConveyorType.eTransportPipe;
//							break;
//						}

						if (meGetType == ConveyorEntity.eConveyorType.eConveyor) continue;//We have no pipes and thus cannot upgrade the pipe

						if (lHopper.TryExtractCubes(this, eCubeTypes.Conveyor, lConveyor, 1))
						{
							lBuildType = ConveyorEntity.eConveyorType.eConveyor;
							break;							
						}

//						if (lHopper.RemoveInventoryCube(eCubeTypes.Conveyor,lConveyor,1) == 1) 	
//						{
//							lBuildType = ConveyorEntity.eConveyorType.eConveyor;
//							break;
//						}
					}

					if (lBuildType == ConveyorEntity.eConveyorType.eNumConveyorTypes)
					{
						//We have failed to locate any resources capable of upgrading this line
						//Note : if we fail to upgrade, we still move onto the next vox
					}
					else
					{

						//attempt to upgrade this type! (I say 'attempt', I've no idea why it would fail)

						//get t2?
						//get non basic?

						WorldScript.instance.BuildOrientationFromEntity(checkSegment, mnSearchX,mnSearchY,mnSearchZ, eCubeTypes.Conveyor, (ushort)lBuildType, mConveyorFlags);
						mnConversions++;

					}
				}
				else
				{
					//This was either a conveyor of T2, an assembly machine, or we lacked resources to upgrade it
				}


				mnSearchDistance++;

				mnSearchDirX = (int)mConveyorForward.x;
				mnSearchDirY = (int)mConveyorForward.y;
				mnSearchDirZ = (int)mConveyorForward.z;

				//in either situation, we move onto the next square now
				mnSearchX += mnSearchDirX;
				mnSearchY += mnSearchDirY;
				mnSearchZ += mnSearchDirZ;
			}
			else
			{
				if (mnSearchDistance == 0) mbNotFacingConveyor = true;
				//not an upgradeable type; we're done!
				mbUpgradeComplete = true;
			}

		}
		else
		{
			//sad face - try again next update (too many repeated fails == give up?)
		}
	}
	public byte mConveyorFlags;
	ConveyorEntity.eConveyorType meGetType;
	Vector3 mConveyorForward;
	Segment checkSegment;
	// ****************************************************************************************************
	bool GetCube(long checkX, long checkY, long checkZ)
	{
		mLastCube = eCubeTypes.NULL;
		checkSegment = null;
		
		if (mFrustrum != null)
		{
			checkSegment = AttemptGetSegment(checkX, checkY, checkZ);
			
			if (checkSegment == null)
				return false;
		}
		else
		{
			// we don't have a frustrum :(
			checkSegment = WorldScript.instance.GetSegment(checkX, checkY, checkZ);
			
			if (checkSegment == null || !checkSegment.mbInitialGenerationComplete || checkSegment.mbDestroyed)
			{
				// postpone doing this again by quite a while, low chance that we'll be available soon
				mrScanDelay = 1.0f;
				return false;
			}
		}				
		
		ushort lCube = checkSegment.GetCube(checkX, checkY, checkZ);
		mLastCube = lCube;

		if (lCube == eCubeTypes.Conveyor)
		{
			CubeData lData = checkSegment.GetCubeData(checkX, checkY, checkZ);

			meGetType = (ConveyorEntity.eConveyorType)lData.mValue;
			mConveyorFlags = lData.meFlags;

			mConveyorForward = SegmentCustomRenderer.GetRotationQuaternion(mConveyorFlags) * Vector3.forward;
			mConveyorForward.Normalize();

		}
		else
		{
			meGetType = ConveyorEntity.eConveyorType.eNumConveyorTypes;
		}


		return true; // return true means we handled this cube.
	}
	// ****************************************************************************************************
	StorageMachineInterface[] maAttachedHoppers;
	public int mnNumValidAttachedHoppers;
	public int mnNumInvalidAttachedHoppers;

	void UpdateAttachedHoppers(bool lbInput)
	{
		int lnNextHopper = 0;
		
		for (int i = 0; i < 6; i++)
		{
			long CheckX = this.mnX;
			long CheckY = this.mnY;
			long CheckZ = this.mnZ;
			
			if (i == 0) CheckX--;
			if (i == 1) CheckX++;
			if (i == 2) CheckY--;
			if (i == 3) CheckY++;
			if (i == 4) CheckZ--;
			if (i == 5) CheckZ++;
			
			Segment targetSegment = AttemptGetSegment(CheckX, CheckY, CheckZ);
			
			if (targetSegment == null)
				continue;//I hope the next segment is ok :-)

			StorageMachineInterface storageMachine = targetSegment.SearchEntity(CheckX, CheckY, CheckZ) as StorageMachineInterface;

			if (storageMachine != null)
			{
				mnNumInvalidAttachedHoppers++;

				eHopperPermissions permissions = storageMachine.GetPermissions();

				if (permissions == eHopperPermissions.Locked) continue;
				if (lbInput == false && permissions == eHopperPermissions.AddOnly) continue;
				if (lbInput == true  && permissions == eHopperPermissions.RemoveOnly) continue;
				if (lbInput == true  && storageMachine.IsFull()) continue;
				if (lbInput == false && storageMachine.IsEmpty()) continue;//we want to get OUT, but there's nothing here
				maAttachedHoppers[lnNextHopper] = storageMachine;
				mnNumInvalidAttachedHoppers--;//it's valid!
				lnNextHopper++;
			}
		}
		mnNumValidAttachedHoppers = lnNextHopper;
	}
	//******************** PowerConsumerInterface **********************
	public float mrCurrentPower;
	public float mrMaxPower = 512;
	public float mrMaxTransferRate = 100;
	
	public float GetRemainingPowerCapacity()
	{
		return mrMaxPower - mrCurrentPower;
	}
	
	public float GetMaximumDeliveryRate()
	{
		return mrMaxTransferRate;
	}
	
	public float GetMaxPower()
	{
		return mrMaxPower;
	}
	
	public bool DeliverPower(float amount)//to what? O.o
	{
		
		if (amount > GetRemainingPowerCapacity())
			return false;
		
		mrCurrentPower += amount;
		MarkDirtyDelayed();
		return true;
	}
	
	public bool WantsPowerFromEntity(SegmentEntity entity)
	{
		return true;
	}		
	/****************************************************************************************/
}
