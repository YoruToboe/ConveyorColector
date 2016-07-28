using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConveryorColector
{
    public class ConveyorColectorMod : FortressCraftMod
    {
        public override ModRegistrationData Register()
        {
            ModRegistrationData modRegistrationData = new ModRegistrationData();
            modRegistrationData.RegisterEntityHandler("Yoru.ConveyorColector");


            Debug.Log("Conveyor Collector Mod v1.0 registered");

            return modRegistrationData;
        }
        public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
        {
            ModCreateSegmentEntityResults result = new ModCreateSegmentEntityResults();

            foreach (ModCubeMap cubeMap in ModManager.mModMappings.CubeTypes)
            {
                if (cubeMap.CubeType == parameters.Cube)
                {
                    if (cubeMap.Key.Equals("Yoru.ConveyorColector"))
                        result.Entity = new MSInventoryPanel(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk);
                }
            }
            return result;
        }
    }
}
