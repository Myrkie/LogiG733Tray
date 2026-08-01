using Android.Graphics;
using Android.Views;

namespace LogiTrayAndroid.Services
{
    class DarkSpinnerAdapter(ArrayAdapter<string> adapter) : BaseAdapter, ISpinnerAdapter
    {
        public override int Count => adapter.Count;

        public override Java.Lang.Object GetItem(int position) => adapter.GetItem(position);

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = adapter.GetView(position, convertView, parent);

            if (view is not TextView tv) return view;
            tv.SetTextColor(Color.White);
            tv.SetBackgroundColor(Color.ParseColor("#202124"));
            tv.SetPadding(20, 20, 20, 20);

            return view;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var view = adapter.GetDropDownView(position, convertView, parent);

            if (view is not TextView tv) return view;
            tv.SetTextColor(Color.White);
            tv.SetBackgroundColor(Color.ParseColor("#202124"));
            tv.SetPadding(20, 20, 20, 20);

            return view;
        }
    }
}