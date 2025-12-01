using System.Windows.Forms;

namespace tickMeter.Classes
{
    public class ListViewNF : ListView
    {
        // Событие прокрутки
        public event ScrollEventHandler Scroll;
        
        private const int WM_VSCROLL = 0x115;
        private const int WM_HSCROLL = 0x114;
        
        public ListViewNF()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
        }

        protected override void OnNotifyMessage(Message m)
        {
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }
        
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            
            // Отслеживаем вертикальную и горизонтальную прокрутку
            if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL)
            {
                Scroll?.Invoke(this, new ScrollEventArgs((ScrollEventType)(m.WParam.ToInt32() & 0xFFFF), 0));
            }
        }
    }
}
