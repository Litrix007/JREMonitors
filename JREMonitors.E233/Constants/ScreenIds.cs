namespace JREMonitors.E233.Constants
{
    public static class ScreenIds
    {
        public static readonly string Meter = nameof(Meter);
        public static readonly string Tid = nameof(Tid);

        #region TIMS

        /// 番台確認
        public static readonly string X00AA = nameof(X00AA);

        /// 準備中
        public static readonly string S00AA = nameof(S00AA);

        /// 初期選択
        public static readonly string S00AB = nameof(S00AB);

        /// この表示器をＴＩＭＳ表示器に切り替える場合は、左端の表示器に必ず保安表示灯を表示して下さい。
        public static readonly string TidChangeToTIMSWarning = nameof(TidChangeToTIMSWarning);

        /// 運転士メニュー
        public static readonly string D00AA = nameof(D00AA);

        /// 車掌メニュー
        public static readonly string C00AA = nameof(C00AA);

        /// 車掌情報
        public static readonly string C01AA = nameof(C01AA);

        /// 車掌情報
        public static readonly string C01AB = nameof(C01AB);

        /// 運転情報
        public static readonly string D01AX = nameof(D01AX);

        /// 車両情報
        public static readonly string D02AA = nameof(D02AA);

        /// ブレーキ確認情報
        public static readonly string D05AA = nameof(D05AA);

        /// ブレーキ確認情報
        public static readonly string D05AB = nameof(D05AB);

        #endregion
    }
}