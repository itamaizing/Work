using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff.AirElemental
{
    public class Light : Skill
    {
        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerator CastJob()
        {
            throw new NotImplementedException();
        }

        protected override void ClearData()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
        {
            throw new NotImplementedException();
        }
    }
}