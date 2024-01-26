using CC1101.NET.Enums;
using CC1101.NET.Interfaces;
using CC1101.NET.Internal.Enums;

namespace CC1101.NET.Internal
{
    internal class WakeOnRadio : IWakeOnRadio
    {
        private readonly IPowerstate _powerstate;
        private readonly SPICommunication _spiCommunication;
        #region constructor

        public WakeOnRadio(IPowerstate powerstate, SPICommunication spiCommunication)
        {
            _powerstate = powerstate;
            _spiCommunication = spiCommunication;
        }

        #endregion

        public void Enable()
        {
            _powerstate.Idle();
            
            _spiCommunication.WriteRegister((byte)DeviceRegister.MCSM0,  0x18);//FS Autocalibration
            _spiCommunication.WriteRegister((byte)DeviceRegister.MCSM2,  0x01);//MCSM2.RX_TIME = 1b

            _spiCommunication.WriteRegister((byte)DeviceRegister.WOREVT1,  0xFF);//High byte Event0 timeout
            _spiCommunication.WriteRegister((byte)DeviceRegister.WOREVT0,  0x7F);//Low byte Event0 timeout
            
            _spiCommunication.WriteRegister((byte)DeviceRegister.WORCTRL,  0x78);//WOR_RES=0b; tEVENT1=0111b=48d -> 48*(750/26MHz)= 1.385ms
            
            _spiCommunication.WriteStrobe(CommandStrobe.SFRX);//flush RX buffer
            _spiCommunication.WriteStrobe(CommandStrobe.SWORRST);//resets the WOR timer to the programmed Event 1
            _spiCommunication.WriteStrobe(CommandStrobe.SWOR);//put the radio in WOR mode when CSn is released
        }

        public void Disable()
        {
            _powerstate.Idle(); //exit WOR Mode
            _spiCommunication.WriteRegister((byte)DeviceRegister.MCSM2,  0x07);  //stay in RX. No RX timeout
        }

        public void Reset()
        { 
            _powerstate.Idle(); //go to IDLE
            
            _spiCommunication.WriteRegister((byte)DeviceRegister.MCSM2,  0x01);//MCSM2.RX_TIME = 1b
            
            _spiCommunication.WriteStrobe(CommandStrobe.SFRX);//flush RX buffer
            _spiCommunication.WriteStrobe(CommandStrobe.SWORRST);//resets the WOR timer to the programmed Event 1
            _spiCommunication.WriteStrobe(CommandStrobe.SWOR);//put the radio in WOR mode when CSn is released
        }
    }
}