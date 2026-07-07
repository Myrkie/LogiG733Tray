using Android.Content;
using Android.Graphics;
using Android.Views;

namespace LogiTrayAndroid.Services
{
    class DarkSpinnerAdapter : BaseAdapter, ISpinnerAdapter
    {
        private readonly ArrayAdapter<string> _adapter;
        private readonly Context _context;

        public DarkSpinnerAdapter(Context context, ArrayAdapter<string> adapter)
        {
            _context = context;
            _adapter = adapter;
        }

        public override int Count => _adapter.Count;

        public override Java.Lang.Object GetItem(int position) => _adapter.GetItem(position);

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = _adapter.GetView(position, convertView, parent);

            if (view is TextView tv)
            {
                tv.SetTextColor(Color.White);
                tv.SetBackgroundColor(Color.ParseColor("#202124"));
                tv.SetPadding(20, 20, 20, 20);
            }

            return view;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var view = _adapter.GetDropDownView(position, convertView, parent);

            if (view is TextView tv)
            {
                tv.SetTextColor(Color.White);
                tv.SetBackgroundColor(Color.ParseColor("#202124"));
                tv.SetPadding(20, 20, 20, 20);
            }

            return view;
        }
    }
}