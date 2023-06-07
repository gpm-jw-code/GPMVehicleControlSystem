using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.AGVDispatch;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {
        private void EventsRegist() //TODO EventRegist
        {
            AGVSMessageFactory.OnVCSRunningDataRequest += GenRunningStateReportData;
            AGVS.OnRemoteModeChanged = AGVSRemoteModeChangeReq;
            AGVC.OnModuleInformationUpdated += CarController_OnModuleInformationUpdated;
            AGVC.OnSickDataUpdated += CarController_OnSickDataUpdated;
            WagoDI.OnEMO += WagoDI_OnEMO;
            WagoDI.OnEMO += AGVC.EMOHandler;
            WagoDI.OnResetButtonPressing += () => ResetAlarmsAsync();
            WagoDI.OnResetButtonPressed += WagoDO.ResetMotor;
            WagoDI.OnResetButtonPressed += WagoDI_OnResetButtonPressed;
            WagoDI.OnFrontArea1LaserTrigger += AGVC.FarArea1LaserTriggerHandler;
            WagoDI.OnBackArea1LaserTrigger += AGVC.FarArea1LaserTriggerHandler;
            WagoDI.OnFrontArea2LaserTrigger += AGVC.FarArea2LaserTriggerHandler;
            WagoDI.OnBackArea2LaserTrigger += AGVC.FarArea2LaserTriggerHandler;

            WagoDI.OnFrontArea1LaserRecovery += AGVC.FrontFarArea1LaserRecoveryHandler;
            WagoDI.OnFrontArea2LaserRecovery += AGVC.FrontFarArea2LaserRecoveryHandler;
            WagoDI.OnBackArea1LaserRecovery += AGVC.BackFarArea1LaserRecoveryHandler;
            WagoDI.OnBackArea2LaserRecovery += AGVC.BackFarArea2LaserRecoveryHandler;

            WagoDI.OnFrontArea1LaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            WagoDI.OnFrontArea2LaserTrigger += WagoDI_OnNearAreaLaserTrigger;
            WagoDI.OnBackArea1LaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            WagoDI.OnBackArea2LaserTrigger += WagoDI_OnNearAreaLaserTrigger;

            WagoDI.OnFrontArea1LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            WagoDI.OnBackArea1LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            WagoDI.OnFrontArea2LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            WagoDI.OnBackArea2LaserRecovery += WagoDI_OnFarAreaLaserRecovery;

            WagoDI.OnFrontNearAreaLaserTrigger += WagoDI_OnNearAreaLaserTrigger;
            WagoDI.OnBackNearAreaLaserTrigger += WagoDI_OnNearAreaLaserTrigger;

            WagoDI.OnFrontNearAreaLaserRecovery += WagoDI_OnNearAreaLaserRecovery;
            WagoDI.OnBackNearAreaLaserRecovery += WagoDI_OnNearAreaLaserRecovery;

            Navigation.OnDirectionChanged += Navigation_OnDirectionChanged;

            clsTaskDownloadData.OnCurrentPoseReq = CurrentPoseReqCallback;

            //AGVC.OnTaskActionFinishAndSuccess += AGVMoveTaskActionSuccessHandle;
            //AGVC.OnTaskActionFinishCauseAbort += CarController_OnTaskActionFinishCauseAbort;
            //AGVC.OnTaskActionFinishButNeedToExpandPath += AGVC_OnTaskActionFinishButNeedToExpandPath ;
            //AGVC.OnMoveTaskStart += CarController_OnMoveTaskStart;

            AGVS.OnTaskDownload += AGVSTaskDownloadConfirm;
            AGVS.OnTaskResetReq = AGVSTaskResetReqHandle;
            AGVS.OnTaskDownloadFeekbackDone += ExecuteAGVSTask;
            Navigation.OnTagReach += OnTagReachHandler;
            BarcodeReader.OnTagLeave += OnTagLeaveHandler;


            AGVC.OnCSTReaderActionDone += CSTReader.UpdateCSTIDDataHandler;


        }
    }
}
