import {
  $ as H,
  a4 as h,
  a6 as x,
  a2 as B,
  a7 as w,
  aa as V,
  ag as z,
  a3 as L,
  a0 as k,
  ak as R,
  al as N,
  a5 as J,
  am as P,
  an as b,
  k as Z,
  a8 as W,
  a9 as Y,
} from "./index.dd282f03.js";
const G = [
  0, 0, 41, 0, 0, 0, 53, 0, 0, 0, 43, 0, 0, 0, 57, 0, 0, 0, 225, 0, 0, 0, 224,
  0, 0, 0, 30, 0, 0, 0, 20, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 58, 0, 0, 0, 31, 0,
  0, 0, 26, 0, 0, 0, 22, 0, 0, 0, 29, 0, 0, 0, 227, 0, 0, 0, 59, 0, 0, 0, 32, 0,
  0, 0, 8, 0, 0, 0, 7, 0, 0, 0, 27, 0, 0, 0, 226, 0, 0, 0, 60, 0, 0, 0, 33, 0,
  0, 0, 21, 0, 0, 0, 9, 0, 0, 0, 6, 0, 0, 0, 61, 0, 0, 0, 34, 0, 0, 0, 23, 0, 0,
  0, 10, 0, 0, 0, 25, 0, 0, 0, 62, 0, 0, 0, 35, 0, 0, 0, 28, 0, 0, 0, 11, 0, 0,
  0, 5, 0, 0, 0, 44, 0, 0, 0, 63, 0, 0, 0, 36, 0, 0, 0, 24, 0, 0, 0, 13, 0, 0,
  0, 17, 0, 0, 0, 64, 0, 0, 0, 37, 0, 0, 0, 12, 0, 0, 0, 14, 0, 0, 0, 16, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 65, 0, 0, 0, 38, 0, 0, 0, 18, 0, 0, 0,
  15, 0, 0, 0, 54, 0, 0, 0, 0, 0, 0, 0, 66, 0, 0, 0, 39, 0, 0, 0, 19, 0, 0, 0,
  51, 0, 0, 0, 55, 0, 10, 1, 0, 0, 0, 0, 67, 0, 0, 0, 45, 0, 0, 0, 47, 0, 0, 0,
  52, 0, 0, 0, 56, 0, 0, 0, 228, 0, 0, 0, 68, 0, 0, 0, 46, 0, 0, 0, 48, 0, 0, 0,
  0, 0, 0, 0, 229, 0, 0, 0, 80, 0, 0, 0, 69, 0, 0, 0, 42, 0, 0, 0, 49, 0, 0, 0,
  40, 0, 0, 0, 82, 0, 0, 0, 81, 0, 0, 0, 73, 0, 0, 0, 76, 0, 0, 0, 75, 0, 0, 0,
  78, 0, 0, 0, 0, 0, 0, 0, 79, 0, 3, 0, 226, 0, 3, 0, 233, 0, 3, 0, 234, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
];
class Q extends H {
  canControlSingleKey = !0;
  type = "keyboard";
  FEA_CMD_SET_RESERT = 1;
  FEA_CMD_SET_REPORT = 3;
  FEA_CMD_SET_PROFILE = 4;
  FEA_CMD_SET_DEBOUNCE = 6;
  FEA_CMD_SET_LEDPARAM = 7;
  FEA_CMD_SET_SLEDPARAM = 8;
  FEA_CMD_SET_KBOPTION = 9;
  FEA_CMD_SET_KEYMATRIX = 10;
  FEA_CMD_SET_MACRO = 11;
  FEA_CMD_SET_USERPIC = 12;
  FEA_CMD_SET_FN = 16;
  FEA_CMD_SET_SLEEPTIME = 17;
  FEA_CMD_SET_USERGIF = 18;
  FEA_CMD_SET_CMD_AUTOOSEN = 23;
  FEA_CMD_SET_MAGNETISM_REPOR = 27;
  FEA_CMD_SET_MAGNETISM_CAL = 28;
  FEA_CMD_SET_MAGNETISM_MAXIMUM_CALIBRATION = 30;
  FEA_CMD_SETTFTLCDDATA = 37;
  FEA_CMD_SET_OLEDCLOCK = 40;
  FEA_CMD_SET_SCREEN_24BITDATA = 41;
  FEA_CMD_SET_SKU = 80;
  FEA_CMD_SET_MULTI_MAGNETISM = 101;
  FEA_CMD_SET_FLASHCHIPERASSE = 172;
  FEA_CMD_GET_RF_VERSION = 128;
  FEA_CMD_GET_REPORT = 131;
  FEA_CMD_GET_PROFILE = 132;
  FEA_CMD_GET_LEDONOFF = 133;
  FEA_CMD_GET_DEBOUNCE = 134;
  FEA_CMD_GET_LEDPARAM = 135;
  FEA_CMD_GET_SLEDPARAM = 136;
  FEA_CMD_GET_KBOPTION = 137;
  FEA_CMD_GET_KEYMATRIX = 138;
  FEA_CMD_GET_MACRO = 139;
  FEA_CMD_GET_USERPIC = 140;
  FEA_CMD_GET_USB_VERSION = 143;
  FEA_CMD_GET_FN = 144;
  FEA_CMD_GET_SLEEPTIME = 145;
  FEA_CMD_GET_CMD_AUTOOSEN = 151;
  FEA_CMD_GET_MULTI_MAGNETISM = 229;
  FEA_CMD_GETTFTLCDDATA = 165;
  FEA_CMD_GET_SCREEN_24BITDATA = 169;
  FEA_CMD_GET_SKU = 208;
  defaultMatrix = G;
  defaultFnMatrix = G;
  defaultFnMacMatrix = G;
  setOldKBOption = async (t, e) => !1;
  getOldKBOption = async (t) => {};
  getDeviceIsBoot = async () => !1;
  isMagnetism = !!(
    this.deviceType.type === "keyboard" && this.deviceType.magnetism
  );
  支持更换轴体 = !!(
    this.deviceType.type === "keyboard" &&
    this.deviceType.other?.isSwitchReplaceable
  );
  get支持顶部死区 = () =>
    (this.deviceType.version.rf !== void 0 &&
      this.deviceType.version.rf >= 1024) ||
    (this.deviceType.version.usb !== void 0 &&
      this.deviceType.version.usb >= 1024);
  get磁轴行程步进倍数 = () => {
    const t = this.deviceType.version.usb,
      i = this.deviceType.version.rf || t;
    return i === void 0
      ? 10
      : i >= 768 && i < 1280
        ? 100
        : i >= 1280
          ? 200
          : 10;
  };
  reset = async (t = !0) => {
    const e = new Uint8Array(64);
    if (
      ((e[0] = this.FEA_CMD_SET_RESERT), (this.上一笔的磁轴数据 = void 0), t)
    ) {
      const i = await this.sendMsg(e, h.Bit7);
      return (await this.vendorSleep(2e3), i);
    }
    return !0;
  };
  getUSBVersion = async () => {
    const t = new Uint8Array(1);
    t[0] = this.FEA_CMD_GET_USB_VERSION;
    const e = await this.commonMsg(t, h.Bit7);
    if (e !== void 0) return (e[8] << 8) | e[7];
  };
  getWangBaReset = async () => {};
  getRFVersion = async () => {
    const t = new Uint8Array(1);
    t[0] = this.FEA_CMD_GET_RF_VERSION;
    const e = await this.commonMsg(t, h.Bit7);
    if (e === void 0) return;
    const i = (e[2] << 8) | e[1];
    if (i !== 0) return i;
  };
  FEA_CMD_GETMLED_VERSION = 174;
  getMledVersion = async () => {
    const t = await this.commonMsg(
      new Uint8Array([this.FEA_CMD_GETMLED_VERSION]),
      h.Bit7,
    );
    if (t === void 0 || t[0] !== this.FEA_CMD_GETMLED_VERSION) return;
    const e = (t[2] << 8) | t[1];
    if (e !== 0) return e;
  };
  FEA_CMD_GETOLED_VERSION = 173;
  getOledVersion = async () => {
    const t = await this.commonMsg(
      new Uint8Array([this.FEA_CMD_GETOLED_VERSION]),
      h.Bit7,
    );
    if (t === void 0 || t[0] !== this.FEA_CMD_GETOLED_VERSION) return;
    const e = (t[2] << 8) | t[1],
      i = (t[4] << 8) | t[3];
    return { oledVersion: e, flashVersion: i };
  };
  getDongleUSBVersion = async () => {
    if (this.deviceType.company !== "cherry") return;
    const t = await this.commonMsgByPath(
      new Uint8Array([240]),
      h.Bit7,
      this.deviceType.devAddr.toString(),
    );
    if (t !== void 0) return (t[2] << 8) | t[1];
  };
  getDongleRFVersion = async () => {
    if (this.deviceType.company !== "cherry") return;
    const t = await this.commonMsgByPath(
      new Uint8Array([128]),
      h.Bit7,
      this.deviceType.devAddr.toString(),
    );
    if (t !== void 0) return (t[2] << 8) | t[1];
  };
  setCurrentProfile = async (t = 0) => {
    const e = new Uint8Array(8);
    ((e[0] = this.FEA_CMD_SET_PROFILE), (e[1] = t));
    const i = await this.sendMsg(e, h.Bit7);
    return (await this.vendorSleep(), i);
  };
  getCurrentProfile = async () => {
    const t = new Uint8Array(64);
    t[0] = this.FEA_CMD_GET_PROFILE;
    const e = await this.commonMsg(t, h.Bit7);
    return e === void 0 ? void 0 : e[1];
  };
  setDeBounce = async (t) => {
    const e = new Uint8Array(8);
    return (
      (e[0] = this.FEA_CMD_SET_DEBOUNCE),
      (e[1] = t),
      (await this.sendMsg(e, h.Bit7)) ? (await this.vendorSleep(), !0) : !1
    );
  };
  getDeBounce = async () => {
    const t = new Uint8Array(8);
    t[0] = this.FEA_CMD_GET_DEBOUNCE;
    const e = await this.commonMsg(t, h.Bit7);
    if (e !== void 0) return e[1];
  };
  setSleepTime = async (t) => {
    const e = new Uint8Array(64);
    e[0] = this.FEA_CMD_SET_SLEEPTIME;
    const i = t.time_bt;
    ((e[8] = i - ((i >> 8) << 8)), (e[9] = i >> 8));
    const n = t.time_24;
    ((e[10] = n - ((n >> 8) << 8)), (e[11] = n >> 8));
    const s = t.deepTime_bt;
    ((e[12] = s - ((s >> 8) << 8)), (e[13] = s >> 8));
    const r = t.deepTime_24;
    return (
      (e[14] = r - ((r >> 8) << 8)),
      (e[15] = r >> 8),
      (await this.sendMsg(e, h.Bit7)) ? (await this.vendorSleep(), !0) : !1
    );
  };
  getSleepTime = async () => {
    const t = new Uint8Array(8);
    t[0] = this.FEA_CMD_GET_SLEEPTIME;
    const e = await this.commonMsg(t, h.Bit7),
      i = {
        time_bt: 120,
        time_24: 120,
        deepTime_bt: 28 * 60,
        deepTime_24: 28 * 60,
      };
    if (e === void 0) return i;
    const n = (e[9] << 8) | e[8],
      s = (e[11] << 8) | e[10],
      r = (e[13] << 8) | e[12],
      a = (e[15] << 8) | e[14];
    return (
      (i.time_bt = n),
      (i.time_24 = s),
      (i.deepTime_bt = r),
      (i.deepTime_24 = a),
      i
    );
  };
  _setLightPic = async (t, e) => {
    const i = new Uint8Array(8);
    ((i[0] = this.FEA_CMD_SET_USERPIC), (i[1] = e ?? 0), (i[2] = 255));
    for (let n = 0; n < 7; n++) {
      ((i[3] = n),
        (i[4] = n == 6 ? 378 - 56 * 6 : 56),
        (i[5] = n == 6 ? 1 : 0));
      let s = t.slice(n * 56, (n + 1) * 56);
      if (
        (s.length < 56 && (s = [...s, ...new Array(56 - s.length).fill(0)]),
        !(await this.sendMsg(new Uint8Array([...i, ...s]), h.Bit7)))
      )
        return !1;
    }
    return (await this.vendorSleep(), !0);
  };
  setLightPic = async (t, e) => {
    const i = Math.ceil(this.defaultMatrix.length / 4),
      n = new Array(i * 3).fill(0),
      s = t.map((r) => this.findIndexInDefaultMatrix(r.hid, r.index));
    for (let r = 0; r < s.length; r++) {
      const a = s[r];
      if (a === void 0) continue;
      const o = x.numToRgb(t[r].rgb);
      ((n[a * 3] = o.r), (n[a * 3 + 1] = o.g), (n[a * 3 + 2] = o.b));
    }
    return await this._setLightPic(n, e);
  };
  _getLightPic = async (t) => {
    const e = new Uint8Array(8);
    ((e[0] = this.FEA_CMD_GET_USERPIC), (e[1] = t ?? 0), (e[2] = 255));
    let i = [];
    for (let n = 0; n < 6; n++) {
      e[3] = n;
      const s = await this.commonMsg(e, h.Bit7);
      s === void 0
        ? (i = [...i, ...new Array(64).fill(0)])
        : (i = [...i, ...s]);
    }
    return i;
  };
  findHidWithIndexInMatrix = (t) => {
    const e = this.defaultMatrix.slice(t * 4, t * 4 + 4);
    return B.matrixToHid(e);
  };
  getLightPic = async (t) => {
    const e = await this._getLightPic(t);
    let i = [];
    for (let n = 0; n < e.length; n += 3) {
      const s = x.rgbToNum(e[n], e[n + 1], e[n + 2]),
        r = this.findHidWithIndexInMatrix(Math.floor(n / 3)),
        a = i.filter((o) => o.hid === r);
      r === 0 || r === void 0 || i.push({ hid: r, rgb: s, index: a.length });
    }
    return i;
  };
  getTFTLCDDataRGBImg = async (t, e, i) => {
    const n = new Uint8Array(64);
    ((n[0] =
      t === "16"
        ? this.FEA_CMD_GETTFTLCDDATA
        : this.FEA_CMD_GET_SCREEN_24BITDATA),
      (n[1] = e.currentFrame),
      (n[2] = e.frameNum),
      (n[3] = e.frameDelay));
    const s = e.data.length;
    ((n[4] = s & 255),
      (n[5] = (s & 65535) >> 8),
      (n[6] = 0),
      (n[8] = e.left & 255),
      (n[9] = e.top & 255),
      (n[10] = e.right & 255),
      (n[11] = e.bottom & 255),
      (n[12] = e.left >> 8),
      (n[13] = e.top >> 8),
      (n[14] = e.right >> 8),
      (n[15] = e.bottom >> 8),
      (n[16] = (s & 16777215) >> 16),
      (n[17] = (s & 4294967295) >> 24),
      (n[18] = i ?? 0));
    let r = await this.commonMsg(n, h.Bit7, 10, 100),
      a = 0;
    for (; (r === void 0 || r[1] === 0) && a < 10; )
      (await w(100), a++, (r = await this.commonMsg(n, h.Bit7, 10, 100)));
    return (await w(100), r !== void 0 && r[1] === 1);
  };
  setTFTLCDDataRGBImg = async (t, e, i) => {
    const n = Math.ceil(e.data.length / 56),
      s = new Uint8Array(8);
    ((s[0] =
      t === "16"
        ? this.FEA_CMD_SETTFTLCDDATA
        : this.FEA_CMD_SET_SCREEN_24BITDATA),
      (s[1] = e.currentFrame),
      (s[2] = e.frameNum),
      (s[3] = e.frameDelay));
    for (let r = 0; r < n; r++) {
      let a = [...e.data.slice(r * 56, r * 56 + 56)];
      const o = a.length;
      (o < 56 && (a = [...a, ...new Array(56 - a.length).fill(0)]),
        (s[4] = r & 255),
        (s[5] = r >> 8),
        (s[6] = o));
      const f = new Uint8Array([...s, ...a]),
        c = await this.sendMsg(f, h.Bit7, 1);
      if ((i && i(r / n), !c)) return !1;
    }
    return (i && i(1), !0);
  };
  setOLEDClock = async () => {
    const t = new Uint8Array(64);
    t[0] = this.FEA_CMD_SET_OLEDCLOCK;
    let e = "";
    try {
      const i = window.require("child_process");
      V.getPlatform() !== "mac"
        ? (e = i.spawnSync(
            "powershell.exe",
            ["get-date -format 'yyyy,MM,dd,HH,mm,ss'"],
            { encoding: "utf8" },
          ).stdout)
        : (e = i.execSync('date +"%Y,%m,%d,%H,%M,%S"').toString());
    } catch {}
    if (e != "" && e != null) {
      const i = e.split(","),
        n = Number(i[0]);
      ((t[8] = n >> 8),
        (t[9] = n & 255),
        (t[10] = Number(i[1])),
        (t[11] = Number(i[2])),
        (t[12] = Number(i[3])),
        (t[13] = Number(i[4])),
        (t[14] = Number(i[5])));
    } else {
      const i = new Date(),
        n = i.getFullYear();
      ((t[8] = n >> 8),
        (t[9] = n & 255),
        (t[10] = i.getMonth() + 1),
        (t[11] = i.getDate()),
        (t[12] = i.getHours()),
        (t[13] = i.getMinutes()),
        (t[14] = i.getSeconds()));
    }
    return await this.sendMsg(t, h.Bit7, 0);
  };
  FEA_CMD_SET_OLEDOPTION = 34;
  setOLEDSysInfo = async (t) => {
    const e = Math.round(Number(t.diskSpceAvailable) / 1024 / 1024 / 1024),
      i = Math.round(Number(t.diskSpaceTotal) / 1024 / 1024 / 1024),
      n = Math.round(Number(t.memUsed) / 1024 / 1024 / 1024),
      s = Math.round(Number(t.memTotal) / 1024 / 1024 / 1024),
      r = t.cpuUsage,
      a = t.cpuTemperater,
      o = Math.round(Number(t.netWorkTotalUp) / 1024 / 1024 / 1024),
      f = Math.round(Number(t.netWorkTotalDown) / 1024 / 1024 / 1024),
      c = new Uint8Array(64);
    return (
      (c[0] = this.FEA_CMD_SET_OLEDOPTION),
      (c[8] = e & 255),
      (c[9] = e >> 8),
      (c[10] = i & 255),
      (c[11] = i >> 8),
      (c[12] = n & 255),
      (c[13] = n >> 8),
      (c[14] = s & 255),
      (c[15] = s >> 8),
      (c[16] = r),
      (c[17] = a),
      (c[18] = o & 255),
      (c[19] = o >> 8),
      (c[20] = f & 255),
      (c[21] = f >> 8),
      await this.sendMsg(c, h.Bit7)
    );
  };
  FEA_CMD_SET_OLEDLUANGAGE = 39;
  setOLEDLanguageSwitch = async (t) =>
    (await this.sendMsg(
      new Uint8Array([this.FEA_CMD_SET_OLEDLUANGAGE, t ? 1 : 0]),
      h.Bit7,
    ))
      ? (await this.vendorSleep(), !0)
      : !1;
  setFlashChipErase = async () => {
    const t = new Uint8Array(64);
    t[0] = this.FEA_CMD_SET_FLASHCHIPERASSE;
    const e = await this.commonMsg(t, h.Bit7);
    return e === void 0
      ? !1
      : e[0] === this.FEA_CMD_SET_FLASHCHIPERASSE &&
          e[1] === 170 &&
          e[2] === 170 &&
          e[3] === 85 &&
          e[4] === 85;
  };
  MAXSPEED = 4;
  DAZZLE = 8;
  NORMAL = 7;
  COMMONCOLOR = {
    0: 16711680,
    1: 16744448,
    2: 16776960,
    3: 65280,
    4: 65535,
    5: 255,
    6: 16711935,
  };
  LightList = {
    0: "LightOff",
    1: "LightAlwaysOn",
    2: "LightBreath",
    3: "LightNeon",
    4: "LightWave",
    5: "LightRipple",
    6: "LightRaindrop",
    7: "LightSnake",
    8: "LightPressAction",
    9: "LightConverage",
    10: "LightSineWave",
    11: "LightKaleidoscope",
    12: "LightLineWave",
    13: "LightUserPicture",
    14: "LightLaser",
    15: "LightCircleWave",
    16: "LightDazzing",
    17: "LightRainDown",
    18: "LightMeteor",
    19: "LightPressActionOff",
    20: "LightMusicFollow3",
    21: "LightScreenColor",
    22: "LightMusicFollow2",
    23: "LightTrain",
    24: "LightFireWorks",
    25: "LightUserColor",
  };
  setLightSetting = async (t) => {
    const e = new Uint8Array(64);
    e[0] = this.FEA_CMD_SET_LEDPARAM;
    const i = z(this.LightList, t.type) ?? 0;
    let n = t.dazzle;
    const s = n ? this.DAZZLE : this.NORMAL,
      r = n ? 0 : 4;
    let a = (t.option << 4) | s;
    if (
      (t.type === "LightUserPicture" && (a = t.option << 4),
      (t.type === "LightMusicFollow3" || t.type === "LightMusicFollow2") &&
        (a = (t.option << 4) | r),
      t.type === "LightScreenColor" && (a = 0),
      (e[1] = i),
      (e[2] = this.MAXSPEED - t.speed),
      (e[3] = t.bright),
      (e[4] = a),
      "rgb" in t && t.rgb !== void 0)
    ) {
      let f = t.rgb;
      f === 16777215 && (f = 16449530);
      const c = x.numToRgb(f);
      ((e[5] = c.r), (e[6] = c.g), (e[7] = c.b));
    }
    t.type === "LightUserPicture" && ((e[5] = 0), (e[6] = 200), (e[7] = 200));
    const o = await this.sendMsg(e, h.Bit8);
    return (await this.vendorSleep(), !!o);
  };
  getLightSetting = async () => {
    const t = new Uint8Array(64);
    t[0] = this.FEA_CMD_GET_LEDPARAM;
    const e = await this.commonMsg(t, h.Bit7);
    if (e === void 0) return;
    const i = e[1],
      n = this.MAXSPEED - e[2],
      s = e[3],
      r = e[4];
    let a = x.rgbToNum(e[5], e[6], e[7]);
    const o = r >> 4,
      f = r & 15,
      c = (_) => {
        if (_ === this.DAZZLE || _ === this.NORMAL) return;
        const p = this.COMMONCOLOR[_];
        p !== void 0 && (a = p);
      },
      d = f === this.DAZZLE,
      A = f === 0;
    a === 16449530 && (a = 16777215);
    const y = this.LightList[i];
    if (y !== void 0)
      return (
        c(f),
        {
          type: y,
          option: o,
          bright: s,
          speed: n,
          rgb: a,
          dazzle:
            y === "LightMusicFollow3" || y === "LightMusicFollow2" ? A : d,
        }
      );
  };
  setSideLightSetting = async (t) => {
    const e = new Uint8Array(64);
    e[0] = this.FEA_CMD_SET_SLEDPARAM;
    let i = 0,
      n = t.dazzle;
    const s = n ? this.DAZZLE : this.NORMAL,
      r = n ? 0 : 4;
    let a = (t.option << 4) | s;
    switch (t.type) {
      case "LightOff":
        i = 0;
        break;
      case "LightAlwaysOn":
        i = 1;
        break;
      case "LightBreath":
        i = 2;
        break;
      case "LightNeon":
        i = 3;
        break;
      case "LightWave":
        i = 4;
        break;
      case "LightSnake":
        i = 5;
        break;
      case "LightMusicFollow2":
        ((i = 6), (a = (t.option << 4) | r));
        break;
    }
    if (
      ((e[1] = i),
      (e[2] = t.speed),
      (e[3] = t.bright),
      (e[4] = a),
      "rgb" in t && t.rgb !== void 0)
    ) {
      let f = t.rgb;
      f === 16777215 && (f = 16449530);
      const c = x.numToRgb(f);
      ((e[5] = c.r), (e[6] = c.g), (e[7] = c.b));
    }
    const o = await this.sendMsg(e, h.Bit8);
    return (await this.vendorSleep(), !!o);
  };
  getSideLightSetting = async () => {
    const t = new Uint8Array(64);
    t[0] = this.FEA_CMD_GET_SLEDPARAM;
    const e = await this.commonMsg(t, h.Bit7);
    if (e === void 0) return;
    const i = e[1],
      n = e[2],
      s = e[3],
      r = e[4];
    let a = x.rgbToNum(e[5], e[6], e[7]);
    const o = r >> 4,
      f = r & 15,
      c = (p) => {
        if (p === this.DAZZLE || p === this.NORMAL) return;
        const v = this.COMMONCOLOR[p];
        v !== void 0 && (a = v);
      },
      d = f === this.DAZZLE,
      A = f === 0;
    a === 16449530 && (a = 16777215);
    const _ = {
      0: { type: "LightOff", option: 0 },
      1: { type: "LightAlwaysOn", option: 0 },
      2: { type: "LightBreath", option: 0 },
      3: { type: "LightNeon", option: o },
      4: { type: "LightWave", option: o },
      5: { type: "LightSnake", option: o },
      6: { type: "LightMusicFollow2", option: o },
    }[i];
    if (_ !== void 0)
      return (
        c(f),
        {
          ..._,
          option: o,
          bright: s,
          speed: n,
          rgb: a,
          dazzle:
            _.type === "LightMusicFollow3" || _.type === "LightMusicFollow2"
              ? A
              : d,
        }
      );
  };
  findIndexInDefaultMatrix = (t, e = 0) => {
    const i = B.hidToMatrix(t);
    let n = 0;
    for (let s = 0; s < this.defaultMatrix.length; s += 4)
      if (L(i, this.defaultMatrix.slice(s, s + 4))) {
        if (e === n) return s / 4;
        n++;
      }
  };
  matrixToConfigValues = async (t) => {
    if (t.length !== this.defaultMatrix.length) return [];
    const e = B.matrixToConfigs(this.defaultMatrix, t);
    for (let i = 0; i < e.length; i++) {
      let n = e[i];
      if (n.type !== "ConfigMacro" || n.macroIndex === void 0) continue;
      let s;
      const r = await this.getMacro(n.macroIndex);
      (r !== void 0 && (s = r),
        (n = {
          ...n,
          macro: s === void 0 ? [] : s.macroEvents,
          repeatCount: s === void 0 ? 0 : s.repeatCount,
        }),
        (e[i] = n));
    }
    return e;
  };
  getMacro = async (t) => {
    let e = new Uint8Array(0);
    const i = new Uint8Array(64);
    ((i[0] = this.FEA_CMD_GET_MACRO), (i[1] = t));
    for (let n = 0; n < 4; n++) {
      i[2] = n;
      const s = await this.commonMsg(i, h.Bit7);
      if (s === void 0) return;
      if (((e = new Uint8Array([...e, ...s])), j([...s]))) break;
    }
    return this.buffToMacroEvents(e);
  };
  buffToMacroEvents = (t) => {
    const e = [],
      i = new DataView(t.buffer).getUint16(0, !0);
    let n = 2,
      s = t.slice(n, n + 4);
    for (n += 4; n < t.length; ) {
      if (s[0] === 249) {
        const r = new Int8Array(s.buffer);
        e.push({ type: "mouse_move", dx: r[2], dy: r[3] });
        let a = 0;
        if (s[1] !== 0) a = s[1] >> 1;
        else {
          const o = t.slice(n, n + 2);
          ((n += 2), (a = new DataView(o.buffer).getUint16(0, !0)));
        }
        e.push({ type: "delay", value: a });
      } else {
        if (s[0] <= 239 && s[0] >= 4)
          e.push({
            type: "keyboard",
            action: s[1] <= 127 ? "up" : "down",
            value: s[0],
          });
        else {
          let a;
          switch (s[0]) {
            case 240:
              a = k.Left;
              break;
            case 241:
              a = k.Right;
              break;
            case 242:
              a = k.Middle;
              break;
            case 243:
              a = k.Back;
              break;
            case 244:
              a = k.Forward;
              break;
          }
          a !== void 0 &&
            e.push({
              type: "mouse_button",
              action: s[1] <= 127 ? "up" : "down",
              value: a,
            });
        }
        let r = 0;
        (s[1] & 127
          ? ((r = s[1] & 127), (n -= 2))
          : (r = new DataView(s.buffer).getUint16(2, !0)),
          e.push({ type: "delay", value: r }));
      }
      if (((s = t.slice(n, n + 4)), L([...s], [0, 0, 0, 0]))) break;
      n += 4;
    }
    return {
      macroEvents: e.filter((r) => !(r.type === "delay" && r.value === 0)),
      repeatCount: i,
    };
  };
  async _getKeyMatrix(t, e) {
    const i = new Uint8Array(64);
    ((i[0] = this.FEA_CMD_GET_KEYMATRIX),
      (i[1] = t),
      (i[2] = 255),
      (i[4] = e ?? 0));
    let n = [];
    for (let s = 0; s < 8; s++) {
      i[3] = s;
      const r = await this.commonMsg(i, h.Bit7);
      r === void 0
        ? (n = [...n, ...new Array(64).fill(0)])
        : (n = [...n, ...r]);
    }
    return n;
  }
  setKeyConfigSimple = async (t, e, i, n) => {
    const s = this.findIndexInDefaultMatrix(t.original, t.index);
    if (s === void 0 || s > 127) return !1;
    const r = B.configToMatrix(t);
    if (r === void 0) return !1;
    const a = new Uint8Array(64);
    ((a[0] = this.FEA_CMD_SET_KEYMATRIX),
      (a[1] = e),
      (a[2] = s),
      (a[5] = Number(n ?? !0)),
      (a[6] = i ?? 0),
      (a[8] = r[0]),
      (a[9] = r[1]),
      (a[10] = r[2]),
      (a[11] = r[3]));
    const o = await this.sendMsg(a, h.Bit7);
    return (
      await this.vendorSleep(),
      t.type === "ConfigMacro" &&
        t.macroIndex !== void 0 &&
        (await this.setMacro(t, t.macroIndex)),
      o
    );
  };
  getKeyConfig = async (t, e) => {
    const i = await this._getKeyMatrix(t, e);
    if (i !== void 0) return await this.matrixToConfigValues([...i]);
  };
  _configsToMatrix = async (t) => {
    const e = [];
    for (let n = 0; n < t.length; n++) {
      const s = t[n],
        r = B.configToMatrix(s);
      e.push({ changeArr: r, orginHid: s.original, index: s.index });
    }
    return this._changeAllConfig(e);
  };
  _changeAllConfig = (t) => {
    let e = [...this.defaultMatrix];
    return (
      t.forEach((i) => {
        const n = this._changeMatrix(i.orginHid, i.changeArr, e, i.index);
        n !== void 0 && (e = n);
      }),
      e
    );
  };
  _changeMatrix = (t, e, i, n) => {
    const s = [...i];
    let r = 2,
      a = !1,
      o = 0;
    const f = new Array();
    for (let c = 0; c < R.length; c++) {
      const d = N(R[c]);
      f.push(d);
    }
    for (; r < this.defaultMatrix.length; r += 4) {
      const c = f.findIndex((d) => L(d, t));
      if (c !== -1 && L(this.defaultMatrix.slice(r - 2, r + 2), R[c]))
        if (n === void 0 || o === n) {
          a = !0;
          break;
        } else {
          o++;
          continue;
        }
      if (L(this.defaultMatrix.slice(r - 2, r + 2), [0, 0, t, 0]))
        if (n === void 0 || o === n) {
          a = !0;
          break;
        } else {
          o++;
          continue;
        }
    }
    if (a)
      return (
        (s[r - 1 - 1] = e[0]),
        (s[r - 1] = e[1]),
        (s[r] = e[2]),
        (s[r + 1] = e[3]),
        s
      );
  };
  _setMacro = async (t, e) => {
    const i = new Uint8Array(8);
    ((i[0] = this.FEA_CMD_SET_MACRO), (i[1] = e));
    let n = 0;
    for (let s = 0; s < 5; s++)
      t.slice(s * 56, 56 + s * 56).some((a) => a !== 0) && n++;
    for (let s = 0; s < n; s++) {
      ((i[2] = s), (i[3] = 56), (i[4] = s === n - 1 ? 1 : 0));
      let r = t.slice(s * 56, 56 + s * 56);
      r.length < 56 && (r = r.concat(new Array(56 - r.length).fill(0)));
      const a = new Uint8Array([...i, ...r]);
      if (!(await this.sendMsg(a, h.Bit7))) return !1;
    }
    return (await this.vendorSleep(), !0);
  };
  setMacro = async (t, e) => {
    const i = t.macro,
      n = new Uint8Array(256);
    let s = 0;
    (n.set([t.repeatCount & 255, t.repeatCount >> 8], 0), (s += 2));
    for (let r = 0; r < i.length; r += 2) {
      const a = i.slice(r, r + 2),
        o = a[0],
        f = a[1];
      if (o.type === "delay" || f.type !== "delay") return !1;
      const c = J.macroEventToByte(o, f);
      (n.set(c, s), (s += c.length));
    }
    return await this._setMacro([...n], e);
  };
  _setKeyConfig = async (t, e, i) => {
    const n = new Uint8Array(8);
    ((n[0] = this.FEA_CMD_SET_KEYMATRIX),
      (n[1] = e ?? 0),
      (n[2] = 255),
      (n[4] = 86),
      (n[6] = i ?? 0));
    const s = Math.ceil(t.length / 56);
    for (let r = 0; r < s; r++) {
      let a = t.slice(r * 56, 56 + r * 56);
      ((n[3] = r),
        (n[4] = a.length),
        (n[5] = r === s - 1 ? 1 : 0),
        a.length < 56 && (a = a.concat(new Array(56 - a.length).fill(0))));
      const o = new Uint8Array([...n, ...a]);
      if (!(await this.sendMsg(o, h.Bit7))) return !1;
    }
    return !0;
  };
  setKeyConfig = async (t, e, i) => {
    const n = await this._configsToMatrix(t);
    for (const r of t)
      r.type == "ConfigMacro" &&
        r.macroIndex !== void 0 &&
        (await this.setMacro(r, r.macroIndex));
    const s = await this._setKeyConfig(n, e, i);
    return (await this.vendorSleep(), s);
  };
  _编码轴体 = (t) => P[t];
  _解码轴体 = (t) => Object.keys(P).find((i) => P[i] === t) || "高特";
  _decodeMagnetismKeyModes = (t) => {
    const e = [];
    for (let i = 0; i < this.defaultMatrix.length; i += 4) {
      let n = 0;
      const s = this.defaultMatrix.slice(i, i + 4);
      if (L(s, [19, 3, 3, 0]) || L(s, [19, 3, 2, 0]) || L(s, [19, 3, 1, 0]))
        continue;
      for (let A = 0; A < i; A += 4)
        L(s, this.defaultMatrix.slice(A, A + 4)) && n++;
      const r = s[0] === 0 && s[1] === 0 && s[3] === 0 ? s[2] : N(s),
        a = Math.floor(i / 4),
        o = t[a],
        f = (o & 128) === 128,
        c = o & 127,
        d = { original: r, index: n, pos: a };
      switch (c) {
        case 0:
          e.push({ ...d, option: "normal", fire: f });
          break;
        case 2:
          e.push({ ...d, option: "dks", fire: f });
          break;
        case 3:
          e.push({ ...d, option: "mt", fire: f });
          break;
        case 4:
          e.push({ ...d, option: "tgl_hold", fire: f });
          break;
        case 5:
          e.push({ ...d, option: "tgl_dots", fire: f });
          break;
        case 7:
          e.push({ ...d, option: "snap", fire: f });
          break;
      }
    }
    return e;
  };
  _getMulitMagnetismCMD = async (t, e, i = 10, n = 10) => {
    const s = new Uint8Array(8);
    ((s[0] = this.FEA_CMD_GET_MULTI_MAGNETISM), (s[1] = t), (s[2] = 1));
    let r = [];
    for (let a = 0; a < e; a++) {
      s[3] = a;
      const o = await this.commonMsg(s, h.Bit7, i, n);
      o === void 0
        ? ((this._callbackJiaoZhunXinXiMark = !1),
          (r = [...r, ...new Array(64).fill(0)]))
        : (r = [...r, ...o]);
    }
    return r;
  };
  _decodeMultiMagnetismTwoByteList = (t) => {
    const e = [];
    for (let i = 0; i < t.length; i += 2) {
      const n = t[i] | (t[i + 1] << 8);
      e.push(n);
    }
    return e;
  };
  getMultiMagnetismInfo = async () => {
    const t = Math.ceil(this.defaultMatrix.length / 4),
      e = await this._getMulitMagnetismCMD(7, 2),
      i = this._decodeMagnetismKeyModes(e),
      n = this._decodeMultiMagnetismTwoByteList(
        await this._getMulitMagnetismCMD(0, 4),
      ),
      s = this._decodeMultiMagnetismTwoByteList(
        await this._getMulitMagnetismCMD(1, 4),
      ),
      r = this._decodeMultiMagnetismTwoByteList(
        await this._getMulitMagnetismCMD(6, 4),
      );
    let a = new Array(t).fill(0),
      o = new Array(t).fill(0);
    i.some((u) => "fire" in u && u.fire) &&
      ((a = this._decodeMultiMagnetismTwoByteList(
        await this._getMulitMagnetismCMD(2, 4),
      )),
      (o = this._decodeMultiMagnetismTwoByteList(
        await this._getMulitMagnetismCMD(3, 4),
      )));
    let f = new Array(t).fill(0),
      c = new Array(t * 4).fill(0);
    i.some((u) => u.option === "dks") &&
      ((f = this._decodeMultiMagnetismTwoByteList(
        await this._getMulitMagnetismCMD(4, 4),
      )),
      (c = await this._getMulitMagnetismCMD(10, 8)));
    let d = new Array(t).fill(0);
    i.some((u) => u.option === "mt") &&
      (d = await this._getMulitMagnetismCMD(5, 2));
    let A = new Array(t).fill(0);
    i.some((u) => u.option === "snap") &&
      (A = await this._getMulitMagnetismCMD(9, 2));
    const y = this.支持更换轴体
        ? await this._getMulitMagnetismCMD(252, 2)
        : new Array(t).fill(0),
      _ = this.get支持顶部死区()
        ? await this._getMulitMagnetismCMD(251, 2)
        : new Array(t).fill(0),
      p = [],
      v = this.get磁轴行程步进倍数();
    for (let u = 0; u < i.length; u++) {
      const g = i[u],
        m = a[g.pos] / v,
        C = o[g.pos] / v,
        S = g.fire,
        F = this.支持更换轴体 ? this._解码轴体(y[g.pos]) : void 0,
        O = { original: g.original, index: g.index };
      switch (g.option) {
        case "normal":
          p.push({
            ...O,
            fire: S,
            firePressTravel: m,
            fireLiftTravel: C,
            轴体: F,
            mode: "normal",
            option: "normal",
            travel: n[g.pos] / v,
            liftTravel: s[g.pos] / v,
            topDeadZoneTravel: this.get支持顶部死区() ? _[g.pos] / v : void 0,
            deadZoneTravel: r[g.pos] / v,
          });
          break;
        case "dks":
          const l = [0, 0, 0, 0];
          for (let D = 0; D < 4; D++) {
            const I = c.slice(D * 128, (D + 1) * 128);
            l[D] = I[g.pos];
          }
          p.push({
            ...O,
            fire: S,
            firePressTravel: m,
            fireLiftTravel: C,
            轴体: F,
            mode: "normal",
            option: "dks",
            dynamicTravel: f[g.pos] / v,
            triggerModes: l,
          });
          break;
        case "mt":
          p.push({
            ...O,
            fire: S,
            firePressTravel: m,
            fireLiftTravel: C,
            轴体: F,
            mode: "normal",
            option: "mt",
            mt_time: d[g.pos] * 10,
          });
          break;
        case "tgl_hold":
          p.push({
            ...O,
            fire: S,
            firePressTravel: m,
            fireLiftTravel: C,
            轴体: F,
            mode: "normal",
            option: "tgl_hold",
          });
          break;
        case "tgl_dots":
          p.push({
            ...O,
            fire: S,
            firePressTravel: m,
            fireLiftTravel: C,
            轴体: F,
            mode: "normal",
            option: "tgl_dots",
          });
          break;
        case "snap":
          const T = A[g.pos],
            E = this.defaultMatrix.slice(T * 4, T * 4 + 4),
            M = E[0] === 0 && E[1] === 0 && E[3] === 0 ? E[2] : N(E);
          p.push({
            ...O,
            fire: S,
            firePressTravel: m,
            fireLiftTravel: C,
            轴体: F,
            mode: "normal",
            option: "snap",
            travel: n[g.pos] / v,
            liftTravel: s[g.pos] / v,
            topDeadZoneTravel: this.get支持顶部死区() ? _[g.pos] / v : void 0,
            deadZoneTravel: r[g.pos] / v,
            bindKeyOriginal: M,
            bindKeyIndex: 0,
          });
          break;
      }
    }
    return p;
  };
  _magnetismModesOptionToNum = (t, e) => {
    let i = 0;
    switch (t) {
      case "normal":
        i = 0;
        break;
      case "dks":
        i = 2;
        break;
      case "mt":
        i = 3;
        break;
      case "tgl_hold":
        i = 4;
        break;
      case "tgl_dots":
        i = 5;
        break;
      case "snap":
        i = 7;
        break;
    }
    return e ? i | 128 : i;
  };
  _magnetismModesOptionToList = (t) => {
    const e = t.map((n) => ({
        option: this._magnetismModesOptionToNum(
          n.option,
          "fire" in n && n.fire,
        ),
        original: n.original,
        index: n.index,
      })),
      i = new Array(this.defaultMatrix.length / 4).fill(0);
    for (const n of e) {
      const s = this.findIndexInDefaultMatrix(n.original, n.index);
      s !== void 0 && (i[s] = n.option);
    }
    return i;
  };
  上一笔的磁轴数据 = {
    options: new Array(),
    travels: new Array(),
    liftTravels: new Array(),
    firePressTravels: new Array(),
    fireLiftTravels: new Array(),
    topDeadZoneTravels: new Array(),
    deadZoneTravels: new Array(),
    dynamicTravels: new Array(),
    mt_times: new Array(),
    bindKeyOriginals: new Array(),
    axisTypes: new Array(),
    dksList: new Array(),
  };
  setMultiMagnetismInfo = async (t) => {
    const e = async (l, T, E) => {
        const M = new Uint8Array(8);
        ((M[0] = this.FEA_CMD_SET_MULTI_MAGNETISM), (M[1] = l), (M[2] = 1));
        const D = Math.ceil(E.length / 56);
        for (let I = 0; I < D; I++) {
          const K = E.slice(I * 56, (I + 1) * 56);
          ((M[3] = I), (M[4] = Number(T && I === D - 1)));
          const X = new Uint8Array([...M, ...K]);
          if (!(await this.sendMsg(X, h.Bit7))) return !1;
        }
        return !0;
      },
      i = Math.ceil(this.defaultMatrix.length / 4),
      n = new Array(this.defaultMatrix.length / 4);
    for (const l of t) {
      const T = this.findIndexInDefaultMatrix(l.original, l.index);
      T !== void 0 && (n[T] = l);
    }
    const s = [],
      r = this.get磁轴行程步进倍数(),
      a = n.map((l) => ("travel" in l ? l.travel * r : 0)),
      o = n.map((l) => ("liftTravel" in l ? l.liftTravel * r : 0)),
      f = n.map((l) => ("firePressTravel" in l ? l.firePressTravel * r : 0)),
      c = n.map((l) => ("fireLiftTravel" in l ? l.fireLiftTravel * r : 0)),
      d = n.map((l) =>
        "deadZoneTravel" in l ? (l.deadZoneTravel ?? 0) * r : 0,
      ),
      A = n.map((l) => ("dynamicTravel" in l ? l.dynamicTravel * r : 0));
    (b(a, this.上一笔的磁轴数据?.travels) || s.push({ op: 0, data: a }),
      b(o, this.上一笔的磁轴数据?.liftTravels) || s.push({ op: 1, data: o }),
      b(f, this.上一笔的磁轴数据?.firePressTravels) ||
        s.push({ op: 2, data: f }),
      b(c, this.上一笔的磁轴数据?.fireLiftTravels) ||
        s.push({ op: 3, data: c }),
      b(d, this.上一笔的磁轴数据?.deadZoneTravels) ||
        s.push({ op: 6, data: d }),
      b(A, this.上一笔的磁轴数据?.dynamicTravels) ||
        s.push({ op: 4, data: A }));
    const y = [],
      _ = n.map((l) => ("mt_time" in l ? l.mt_time / 10 : 0)),
      p = n.map((l) => ("bindKeyOriginal" in l ? l.bindKeyOriginal : 0)),
      v = this.get支持顶部死区()
        ? n.map((l) =>
            "topDeadZoneTravel" in l ? (l.topDeadZoneTravel ?? 0) * r : 0,
          )
        : [];
    if (
      (b(_, this.上一笔的磁轴数据?.mt_times) || y.push({ op: 5, data: _ }),
      !b(p, this.上一笔的磁轴数据?.bindKeyOriginals))
    ) {
      const l = new Array(p.length).fill(0);
      for (let T = 0; T < p.length; T++) {
        const E = p[T];
        if (E === 0) continue;
        const M = this.findIndexInDefaultMatrix(E, 0);
        M !== void 0 && (l[T] = M);
      }
      y.push({ op: 9, data: l });
    }
    b(v, this.上一笔的磁轴数据?.topDeadZoneTravels) ||
      y.push({ op: 251, data: v });
    const u = t.filter((l) => l.option === "dks"),
      g = !b(u, this.上一笔的磁轴数据?.dksList ?? []),
      m = n.map((l) => ("轴体" in l ? this._编码轴体(l.轴体 ?? "高特") : 0)),
      C = !b(m, this.上一笔的磁轴数据?.axisTypes),
      S = n.map((l) =>
        this._magnetismModesOptionToNum(l.option, "fire" in l && l.fire),
      );
    if (!b(S, this.上一笔的磁轴数据?.options)) {
      const l = s.length === 0 && y.length === 0 && !g && !C;
      if (!(await e(7, l, S))) return !1;
    }
    const F = (l, T) => {
        const E = new Array();
        for (let M = 0; M < i; M++) E.push(l & 255, l >> 8);
        for (let M = 0; M < T.length; M++) {
          const D = T[M];
          ((E[M * 2] = D & 255), (E[M * 2 + 1] = D >> 8));
        }
        return E;
      },
      O = (l, T) => {
        const E = new Array(i).fill(l);
        for (let M = 0; M < i; M++) E[M] = T[M];
        return E;
      };
    for (let l = 0; l < s.length; l++) {
      const T = s[l],
        E = y.length === 0 && l === s.length - 1 && !g;
      let M = 0;
      switch (T.op) {
        case 0:
          M = 200;
          break;
        case 1:
          M = 280;
          break;
        case 6:
          M = 30;
          break;
        default:
          M = 0;
          break;
      }
      if (!(await e(T.op, E, F(M, T.data)))) return !1;
    }
    for (let l = 0; l < y.length; l++) {
      const T = y[l],
        E = l === y.length - 1 && !g;
      if (!(await e(T.op, E, O(T.op === 251 ? 30 : 0, T.data)))) return !1;
    }
    if (g)
      for (let l = 0; l < u.length; l++) {
        const T = u[l],
          E = this.findIndexInDefaultMatrix(T.original, T.index);
        if (E === void 0) continue;
        if (
          !(await this._sendMagnetismInfoSimpleCMD(
            8,
            E,
            l === u.length - 1,
            T.triggerModes ?? [0, 0, 0, 0],
          ))
        )
          return !1;
      }
    return (
      await this.vendorSleep(500),
      C && !(await e(252, !0, m))
        ? !1
        : ((this.上一笔的磁轴数据 = {
            options: S,
            travels: a,
            liftTravels: o,
            firePressTravels: f,
            fireLiftTravels: c,
            topDeadZoneTravels: v,
            deadZoneTravels: d,
            dynamicTravels: A,
            mt_times: _,
            bindKeyOriginals: p,
            axisTypes: m,
            dksList: u,
          }),
          await this.vendorSleep(),
          !0)
    );
  };
  _sendMagnetismInfoSimpleCMD = async (t, e, i, n) => {
    const s = new Uint8Array(8);
    ((s[0] = this.FEA_CMD_SET_MULTI_MAGNETISM),
      (s[1] = t),
      (s[2] = 0),
      (s[3] = e),
      (s[4] = i ? 1 : 0));
    const r = new Uint8Array([...s, ...n]);
    return !!(await this.sendMsg(r, h.Bit7));
  };
  setMagnetismInfoSimple = async (t, e) => {
    const i = this.findIndexInDefaultMatrix(t.original, t.index);
    if (i === void 0 || i > 127) return !1;
    const n = [];
    if (
      (e === void 0
        ? ("travel" in t && n.push(0),
          "liftTravel" in t && n.push(1),
          "deadZoneTravel" in t && n.push(6),
          "firePressTravel" in t && t.fire && n.push(2),
          "fireLiftTravel" in t && t.fire && n.push(3),
          "dynamicTravel" in t && n.push(4),
          "triggerModes" in t && n.push(8),
          "mt_time" in t && n.push(5),
          this.get支持顶部死区() && "topDeadZoneTravel" in t && n.push(251))
        : ("travel" in t &&
            (("travel" in e && t.travel !== e.travel) || !("travel" in e)) &&
            n.push(0),
          "liftTravel" in t &&
            (("liftTravel" in e && t.liftTravel !== e.liftTravel) ||
              !("liftTravel" in e)) &&
            n.push(1),
          "deadZoneTravel" in t &&
            (("deadZoneTravel" in e && t.deadZoneTravel !== e.deadZoneTravel) ||
              !("deadZoneTravel" in e)) &&
            n.push(6),
          "firePressTravel" in t &&
            "firePressTravel" in e &&
            t.firePressTravel !== e.firePressTravel &&
            n.push(2),
          "fireLiftTravel" in t &&
            "fireLiftTravel" in e &&
            t.fireLiftTravel !== e.fireLiftTravel &&
            n.push(3),
          "dynamicTravel" in t &&
            (("dynamicTravel" in e && t.dynamicTravel !== e.dynamicTravel) ||
              !("dynamicTravel" in e)) &&
            n.push(4),
          "triggerModes" in t &&
            (("triggerModes" in e &&
              !L(t.triggerModes ?? [], e.triggerModes ?? [])) ||
              !("triggerModes" in e)) &&
            n.push(8),
          "mt_time" in t &&
            (("mt_time" in e && t.mt_time !== e.mt_time) ||
              !("mt_time" in e)) &&
            n.push(5),
          this.get支持顶部死区() &&
            "topDeadZoneTravel" in t &&
            (("topDeadZoneTravel" in e &&
              t.topDeadZoneTravel !== e.topDeadZoneTravel) ||
              !("topDeadZoneTravel" in e)) &&
            n.push(251)),
      t.option !== e?.option || t.fire !== e?.fire)
    ) {
      const r = this._magnetismModesOptionToNum(
        t.option,
        "fire" in t && t.fire,
      );
      if (!(await this._sendMagnetismInfoSimpleCMD(7, i, n.length === 0, [r])))
        return !1;
      n.length === 0 && (await this.vendorSleep());
    }
    const s = this.get磁轴行程步进倍数();
    for (let r = 0; r < n.length; r++) {
      const a = n[r];
      let o = [];
      if (a === 0 && "travel" in t) {
        const c = t.travel * s;
        o = [c & 255, c >> 8];
      }
      if (a === 1 && "liftTravel" in t) {
        const c = t.liftTravel * s;
        o = [c & 255, c >> 8];
      }
      if (a === 6 && "deadZoneTravel" in t) {
        const c = (t.deadZoneTravel ?? 0) * s;
        o = [c & 255, c >> 8];
      }
      if (a === 2 && "firePressTravel" in t) {
        const c = t.firePressTravel * s;
        o = [c & 255, c >> 8];
      }
      if (a === 3 && "fireLiftTravel" in t) {
        const c = t.fireLiftTravel * s;
        o = [c & 255, c >> 8];
      }
      if (a === 4 && "dynamicTravel" in t) {
        const c = t.dynamicTravel * s;
        o = [c & 255, c >> 8];
      }
      if (
        (a === 8 && "triggerModes" in t && (o = t.triggerModes ?? [0, 0, 0, 0]),
        a === 5 && "mt_time" in t && (o = [t.mt_time / 10]),
        a === 251 &&
          "topDeadZoneTravel" in t &&
          (o = [(t.topDeadZoneTravel ?? 0) * s]),
        !(await this._sendMagnetismInfoSimpleCMD(a, i, r === n.length - 1, o)))
      )
        return !1;
    }
    if (
      (n.length !== 0 && (await this.vendorSleep()),
      this.支持更换轴体 &&
        "轴体" in t &&
        t.轴体 !== void 0 &&
        ((e !== void 0 && e.轴体 !== t.轴体) || e === void 0))
    ) {
      if (
        !(await this._sendMagnetismInfoSimpleCMD(252, i, !0, [
          this._编码轴体(t.轴体),
        ]))
      )
        return !1;
      await this.vendorSleep();
    }
    return !0;
  };
  setSnapKeySimple = async (t, e) => {
    const i = this.findIndexInDefaultMatrix(t.original, t.index),
      n = this.findIndexInDefaultMatrix(t.bindKeyOriginal, t.bindKeyIndex);
    return i === void 0 || n === void 0
      ? !1
      : (await this._sendMagnetismInfoSimpleCMD(7, i, !1, [
          this._magnetismModesOptionToNum("snap", "fire" in t && t.fire),
        ]),
        await this._sendMagnetismInfoSimpleCMD(7, n, !1, [
          this._magnetismModesOptionToNum(
            "snap",
            e === void 0 ? !1 : "fire" in e && e.fire,
          ),
        ]),
        await this._sendMagnetismInfoSimpleCMD(9, i, !1, [n]),
        await this._sendMagnetismInfoSimpleCMD(9, n, !0, [i]),
        !0);
  };
  setKBOption = async (t) => {
    const e = new Uint8Array(8);
    ((e[0] = this.FEA_CMD_SET_KBOPTION),
      (e[2] = t.fnIndex),
      (e[3] = Number(t.防误触开关)),
      (e[4] = t.RTStab / 25),
      (e[5] = t.WASDSwap ? 1 : 0));
    const i = await this.sendMsg(e, h.Bit7);
    return (await this.vendorSleep(), i);
  };
  getKBOption = async () => {
    const t = new Uint8Array(8);
    t[0] = this.FEA_CMD_GET_KBOPTION;
    const e = await this.commonMsg(t, h.Bit7);
    if (e === void 0) return;
    const i = e[4] > 5 ? 0 : e[4] * 25,
      n = e[5] === 1;
    let s = "win";
    switch (e[1]) {
      case 1:
        s = "mac";
        break;
      case 2:
        s = "ios";
        break;
      case 3:
        s = "android";
        break;
      default:
        s = "win";
        break;
    }
    const r = e[2];
    return {
      防误触开关: !!e[3],
      RTStab: i,
      WASDSwap: n,
      system: s,
      fnIndex: r,
    };
  };
  getKbPowerSaveMode = async () => {
    const t = await this.commonMsg(
      new Uint8Array([this.FEA_CMD_GET_LEDONOFF]),
      h.Bit7,
    );
    if (t !== void 0) return t[1] === 1;
  };
  setKbPowerSaveMode = async (t) => !1;
  setKeyMagnetismReport = async (t) => {
    const e = new Uint8Array(8);
    ((e[0] = this.FEA_CMD_SET_MAGNETISM_REPOR), (e[1] = Number(t)));
    const i = await this.sendMsg(e, h.Bit7);
    return (await this.vendorSleep(), i);
  };
  _setZuiXiaoXingChengJiaoZhun = async (t) => {
    const e = new Uint8Array(8);
    ((e[0] = this.FEA_CMD_SET_MAGNETISM_CAL), (e[1] = Number(t)));
    const i = await this.sendMsg(e, h.Bit7);
    return (await this.vendorSleep(), i);
  };
  _setZuiDaXingChengJiaoZhun = async (t) => {
    const e = new Uint8Array(8);
    ((e[0] = this.FEA_CMD_SET_MAGNETISM_MAXIMUM_CALIBRATION),
      (e[1] = Number(t)));
    const i = await this.sendMsg(e, h.Bit7);
    return (await this.vendorSleep(), i);
  };
  _getJiaoZhunXinXi = async () => {
    const t = new Array(this.defaultMatrix.length / 4).fill(0),
      e = this._decodeMultiMagnetismTwoByteList(
        await this._getMulitMagnetismCMD(254, 4, 1, 1),
      );
    if (t.length !== e.length) return;
    const i = new Array();
    for (let n = 0; n < this.defaultMatrix.length; n += 4) {
      const s = this.defaultMatrix.slice(n, n + 4),
        r = B.matrixToHid(s);
      if (r === void 0) continue;
      const a = n / 4,
        o = i.filter((f) => f.original === r).length;
      i.push({ 磁通量: t[a], 行程值: e[a], original: r, index: o });
    }
    return i;
  };
  _callbackJiaoZhunXinXiMark = !1;
  _stateJiaoZhunXinXi = "end";
  getJiaoZhunXinXiLoop = async (t, e) => {
    if (((this._callbackJiaoZhunXinXiMark = t), t))
      for (; this._callbackJiaoZhunXinXiMark; ) {
        this._stateJiaoZhunXinXi = "ing";
        const i = await this._getJiaoZhunXinXi();
        (e && e(i), (this._stateJiaoZhunXinXi = "end"));
      }
  };
  setJiaoZhunKaiGuan = async (t, e) => {
    for (
      this._callbackJiaoZhunXinXiMark = !1;
      this._stateJiaoZhunXinXi === "ing";
    )
      await w(10);
    (t
      ? (await this._setZuiXiaoXingChengJiaoZhun(!0),
        await this._setZuiXiaoXingChengJiaoZhun(!1),
        await this._setZuiDaXingChengJiaoZhun(!0))
      : await this._setZuiDaXingChengJiaoZhun(!1),
      this.getJiaoZhunXinXiLoop(t, e));
  };
  _解码fnSys = (t) => {
    switch (t) {
      case "win":
        return 0;
      case "mac":
        return 1;
      case "android":
        return 2;
      case "ios":
        return 3;
      default:
        return 0;
    }
  };
  async _getFnKeyMatrix(t, e) {
    const i = new Uint8Array(64);
    ((i[0] = this.FEA_CMD_GET_FN),
      (i[1] = this._解码fnSys(e)),
      (i[2] = t),
      (i[3] = 255));
    let n = [];
    for (let s = 0; s < 8; s++) {
      i[4] = s;
      const r = await this.commonMsg(i, h.Bit7);
      r === void 0
        ? (n = [...n, ...new Array(64).fill(0)])
        : (n = [...n, ...r]);
    }
    return n;
  }
  getFnKeyConfig = async (t, e) => {
    const i = await this._getFnKeyMatrix(t, e);
    if (i !== void 0) return await this.matrixToConfigValues(i);
  };
  setFnKeyConfigSimple = async (t, e, i) => {
    const n = this.findIndexInDefaultMatrix(t.original, t.index);
    if (n === void 0) return;
    const s = new Uint8Array(64);
    ((s[0] = this.FEA_CMD_SET_FN),
      (s[1] = this._解码fnSys(i)),
      (s[2] = e),
      (s[3] = n));
    const r = i === "mac" ? this.defaultFnMacMatrix : this.defaultFnMatrix;
    let a = B.configToMatrix(t);
    if (
      !(
        a === void 0 ||
        (t.type === "combo" &&
          t.original === t.key &&
          t.original === t.key2 &&
          t.original === t.skey &&
          (a = r.slice(n * 4, 4 + n * 4)),
        (s[8] = a[0]),
        (s[9] = a[1]),
        (s[10] = a[2]),
        (s[11] = a[3]),
        !(await this.sendMsg(s, h.Bit7)))
      )
    )
      return (
        await this.vendorSleep(),
        t.type === "combo" &&
        t.original === t.key &&
        t.original === t.key2 &&
        t.original === t.skey
          ? B.changeArrToConfig(r.slice(n * 4, n * 4 + 4), t.original, t.index)
          : (t.type === "ConfigMacro" &&
              t.macroIndex !== void 0 &&
              (await this.setMacro(t, t.macroIndex), await this.vendorSleep()),
            t)
      );
  };
  _setFnKeyConfig = async (t, e, i) => {
    const n = new Uint8Array(8);
    ((n[0] = this.FEA_CMD_SET_FN),
      (n[1] = this._解码fnSys(i)),
      (n[2] = e),
      (n[3] = 255));
    for (let s = 0; s < 10; s++) {
      ((n[4] = s), (n[5] = s === 9 ? 0 : 56), (n[6] = s === 9 ? 1 : 0));
      let r = t.slice(s * 56, 56 + s * 56);
      r.length < 56 && (r = r.concat(new Array(56 - r.length).fill(0)));
      const a = new Uint8Array([...n, ...r]);
      if (!(await this.sendMsg(a, h.Bit7))) return !1;
    }
    return (await this.vendorSleep(), !0);
  };
  setFnKeyConfig = async (t, e, i) => {
    const n = await this._configsToMatrix(t);
    if (n !== void 0) {
      for (let s = 0; s < t.length; s++) {
        const r = t[s];
        r.type == "ConfigMacro" &&
          r.macroIndex !== void 0 &&
          (await this.setMacro(r, r.macroIndex));
      }
      return await this._setFnKeyConfig(n, e, i);
    }
  };
  setReportRate = async (t) => {
    let e;
    switch (t) {
      case 8e3:
        e = 0;
        break;
      case 4e3:
        e = 1;
        break;
      case 2e3:
        e = 2;
        break;
      case 1e3:
        e = 3;
        break;
      case 500:
        e = 4;
        break;
      case 250:
        e = 5;
        break;
      case 125:
        e = 6;
        break;
    }
    const i = new Uint8Array(8);
    return (
      (i[0] = this.FEA_CMD_SET_REPORT),
      (i[2] = e),
      (await this.sendMsg(i, h.Bit7)) ? (await this.vendorSleep(), !0) : !1
    );
  };
  getReportRate = async () => {
    const t = new Uint8Array(8);
    t[0] = this.FEA_CMD_GET_REPORT;
    const e = await this.commonMsg(t, h.Bit7);
    if (e === void 0) return;
    switch (e[2]) {
      case 0:
        return 8e3;
      case 1:
        return 4e3;
      case 2:
        return 2e3;
      case 3:
        return 1e3;
      case 4:
        return 500;
      case 5:
        return 250;
      case 6:
        return 125;
      default:
        return;
    }
  };
  setAutoOsen = async (t) => {
    const e = new Uint8Array(8);
    return (
      (e[0] = this.FEA_CMD_SET_CMD_AUTOOSEN),
      (e[1] = t ? 1 : 0),
      (await this.sendMsg(e, h.Bit7)) ? (await this.vendorSleep(), !0) : !1
    );
  };
  getAutoOsen = async () => {
    const t = new Uint8Array(8);
    t[0] = this.FEA_CMD_GET_CMD_AUTOOSEN;
    const e = await this.commonMsg(t, h.Bit7);
    return e === void 0 ? !1 : e[1] === 1;
  };
  skuCount = 0;
  skuToText = (t) => "";
  getSku = async () => {
    if (this.deviceType.company !== "TITANHUB") return 0;
    const t = new Uint8Array(64);
    t[0] = this.FEA_CMD_GET_SKU;
    const e = await this.commonMsg(t, h.Bit7);
    return e === void 0 ? 0 : e[1];
  };
  setSku = async (t) => {
    if (this.deviceType.company !== "TITANHUB") return !1;
    const e = new Uint8Array(8);
    return (
      (e[0] = this.FEA_CMD_SET_SKU),
      (e[1] = t),
      await this.sendMsg(e, h.Bit7)
    );
  };
  setUserGifStart = async () => {
    const t = new Uint8Array(8);
    ((t[0] = this.FEA_CMD_SET_USERGIF), (t[3] = 0));
    const e = await this.sendMsg(t, h.Bit7);
    return (await this.vendorSleep(300), e);
  };
  setUserGif = async (t, e) => {
    const i = new Array(8);
    ((i[0] = this.FEA_CMD_SET_USERGIF),
      (i[1] = t.currentFrame),
      (i[4] = t.frameNum),
      (i[5] = t.frameDelay & 255),
      (i[6] = (t.frameDelay >> 8) & 255));
    const n = new Array(Math.ceil(this.defaultMatrix.length / 4) * 3).fill(0);
    for (const s of t.picArr) {
      const r = x.numToRgb(s.rgb),
        a = this.findIndexInDefaultMatrix(s.hid, s.index);
      a !== void 0 &&
        ((n[a * 3] = r.r), (n[a * 3 + 1] = r.g), (n[a * 3 + 2] = r.b));
    }
    for (let s = 0; s < 7; s++) {
      ((i[2] = s), (i[3] = 1));
      let r = n.slice(s * 56, 56 + s * 56);
      r.length < 56 && (r = r.concat(new Array(56 - r.length).fill(0)));
      const a = [...i, ...r];
      if (!(await this.sendMsgNOSleep(new Uint8Array(a), h.Bit7))) return !1;
      await w(5);
    }
    return (e !== void 0 && e(1), !0);
  };
  BOOT_INFOS = [
    { vid: 12625, pid: 20522 },
    { vid: 12625, pid: 16438 },
    { vid: 1130, pid: 302 },
    { vid: 1130, pid: 304 },
  ];
  boot_usb = async () => {
    if (
      this.BOOT_INFOS.some(
        (i) => i.vid === this.deviceType.vid && i.pid === this.deviceType.pid,
      )
    )
      return this.deviceType.devAddr.toString();
    (await this.sendMsg(new Uint8Array([127, 85, 170, 85, 170]), h.Bit7),
      await w(1e3 * 1));
    let t = 0,
      e;
    for (; e === void 0 && t < 100; )
      (await w(500),
        (e = Z.devlistList.find(
          (i) =>
            i.oneofDev.oneofKind === "dev" &&
            i.oneofDev.dev.devType === W.YZWBoot &&
            this.BOOT_INFOS.some(
              (n) =>
                i.oneofDev.oneofKind === "dev" &&
                n.vid === i.oneofDev.dev.vid &&
                n.pid === i.oneofDev.dev.pid,
            ),
        )),
        t++);
    if (!(e === void 0 || e.oneofDev.oneofKind !== "dev"))
      return e.oneofDev.dev.path;
  };
  findSameDevPath = async (t) => {
    let e = 0,
      i;
    for (; i === void 0 && e < 20; ) {
      await w(500);
      const n = Z.devlistList.find(
        (s) =>
          s.oneofDev.oneofKind === "dev" &&
          (s.oneofDev.dev.path === this.deviceType.devAddr ||
            s.oneofDev.dev.id === this.deviceType.id ||
            (t !== void 0 && s.oneofDev.dev.id === t)),
      );
      ((i = n?.oneofDev.oneofKind === "dev" ? n.oneofDev.dev.path : void 0),
        e++);
    }
    if (i !== void 0) return i;
  };
  boot_rf = async () => {
    const t = await this.findSameDevPath(-1);
    if (t === void 0) return;
    const e = await this.commonMsgByPath(new Uint8Array([247]), h.Bit7, t);
    if (e !== void 0 && e[8] === 1 && e[7] === 1) return t;
    (await w(100),
      await this.sendMsgByPath(
        new Uint8Array([248, 85, 170, 85, 170, 0, 0, 130]),
        h.Bit7,
        t,
      ),
      await w(1e3 * 3));
    let i = 0,
      n = !1;
    for (; !n && i < 10; ) {
      i++;
      const s = await this.commonMsgByPath(new Uint8Array([247]), h.Bit7, t);
      ((n = s !== void 0 && s[7] === 1 && s[8] === 1), await w(500));
    }
    if (n) return t;
  };
  upgrade_dev = async (t, e, i, n) => {
    n(0);
    const s = t.slice(i),
      r = 64,
      a = Math.ceil(s.length / r),
      o = new Uint8Array(2);
    ((o[0] = a & 255), (o[1] = a >> 8));
    const f = new Uint8Array(4);
    ((f[0] = s.length & 255),
      (f[1] = (s.length & 65280) >> 8),
      (f[2] = (s.length & 16711680) >> 16),
      (f[3] = (s.length & 4278190080) >> 24));
    const c = [186, 192];
    (o.map((u) => c.push(u)), f.slice(0, 3).map((u) => c.push(u)));
    let d,
      A = 0;
    for (; d === void 0 && A < 10; )
      ((d = await this.commonMsgByPath(new Uint8Array(c), h.None, e)),
        A++,
        await w(1e3));
    if (d === void 0) return !1;
    for (let u = 0; u < a; u++) {
      const g = u * r > s.length ? r - (u * r - s.length) : r,
        m = s.slice(u * r, u * r + g);
      if (!(await this.sendMsgByPath(m, h.None, e))) return !1;
      n(u / a);
    }
    const y = Y([...s]),
      _ = new Uint8Array(4);
    ((_[0] = y & 255),
      (_[1] = (y & 65280) >> 8),
      (_[2] = (y & 16711680) >> 16),
      (_[3] = (y & 4278190080) >> 24));
    const p = [186, 194];
    (o.map((u) => p.push(u)), _.map((u) => p.push(u)), f.map((u) => p.push(u)));
    const v = await this.commonMsgByPath(new Uint8Array(p), h.None, e, 10, 100);
    return v !== void 0 && v[4] === 85;
  };
  upgrade = async (t, e) => {
    if (
      (e(0),
      this.deviceType.isBoot &&
        !this.BOOT_INFOS.some(
          (s) => s.vid === this.deviceType.vid && s.pid === this.deviceType.pid,
        ))
    )
      return !1;
    const i = await this.boot_usb();
    return i === void 0 ? !1 : await this.upgrade_dev(t, i, 20480, e);
  };
  rfUpgrade = async (t, e) => {
    if (
      (e(0),
      this.deviceType.isBoot &&
        this.BOOT_INFOS.some(
          (a) => a.vid === this.deviceType.vid && a.pid === this.deviceType.pid,
        ))
    )
      return !1;
    const i = await this.boot_rf();
    if (i === void 0 || !(await this.upgrade_dev(t, i, 65536, e))) return !1;
    await w(2e3);
    let s = !1,
      r = 0;
    for (; !s && r < 10; ) {
      r++;
      const a = await this.commonMsgByPath(
        new Uint8Array([247]),
        h.Bit7,
        i,
        10,
        100,
      );
      s = a !== void 0 && a[7] === 0 && a[8] === 0;
    }
    return (e(1), s);
  };
  FEA_CMD_SET_OLEDBOOTLOADER = 48;
  FEA_CMD_GET_OLEDBOOTLOADER = 176;
  FEA_CMD_SET_OLEDBOOTSTART = 49;
  FEA_CMD_GET_OLEDBOOTCHECKSUM = 177;
  oledUpgrade = async (t, e) => {
    e(0);
    const i = await this.findSameDevPath();
    if (
      i === void 0 ||
      !(await this.sendMsgByPath(
        new Uint8Array([
          this.FEA_CMD_SET_OLEDBOOTLOADER,
          85,
          170,
          85,
          170,
          0,
          0,
          0,
        ]),
        h.Bit7,
        i,
      ))
    )
      return !1;
    let s = 0,
      r = !1;
    for (; !r && s < 5; ) {
      await w(1e3 * 3);
      const _ = await this.commonMsgByPath(
        new Uint8Array([this.FEA_CMD_GET_OLEDBOOTLOADER]),
        h.Bit7,
        i,
        10,
        100,
      );
      ((r = _ !== void 0 && _[0] === this.FEA_CMD_GET_OLEDBOOTLOADER), s++);
    }
    const a = t.slice(65536),
      o = 64,
      f = Math.ceil(a.length / o),
      c = new Uint8Array(2);
    ((c[0] = f & 255),
      (c[1] = f >> 8),
      await this.sendMsgByPath(
        new Uint8Array([this.FEA_CMD_SET_OLEDBOOTSTART, ...c]),
        h.Bit7,
        i,
      ));
    let d = 0;
    for (let _ = 0; _ < f; _++) {
      const p = _ * o > a.length ? o - (_ * o - a.length) : o,
        v = a.slice(_ * o, _ * o + p),
        u = await this.sendMsgByPath(v, h.None, i);
      for (let g = 0; g < v.length; g++) d += v[g];
      if (u) e(_ / f);
      else return (e(1), !1);
    }
    const A = new Uint8Array(4);
    ((A[0] = d & 255),
      (A[1] = (d & 65280) >> 8),
      (A[2] = (d & 16711680) >> 16),
      (A[3] = (d & 4278190080) >> 24));
    const y = await this.commonMsgByPath(
      new Uint8Array([this.FEA_CMD_GET_OLEDBOOTCHECKSUM, ...A]),
      h.Bit7,
      i,
      10,
      1e3 * 1,
    );
    return (e(1), y !== void 0 && y[1] === 85);
  };
  FEA_CMD_SETTFTFLASHDATA = 50;
  FEA_CMD_GETTFTFLASHDATA = 178;
  flashUpgrade = async (t, e) => {
    e(0);
    const i = await this.findSameDevPath();
    if (i === void 0) return !1;
    const n = t,
      s = 56,
      r = Math.ceil(n.length / s),
      a = await this.commonMsgByPath(
        new Uint8Array([
          this.FEA_CMD_GETTFTFLASHDATA,
          r & 255,
          (r & 65280) >> 8,
        ]),
        h.Bit7,
        i,
      );
    if (a === void 0 || a[0] !== this.FEA_CMD_GETTFTFLASHDATA || a[1] !== 1)
      return !1;
    for (let o = 0; o < r; o++) {
      let f = s;
      (o + 1) * s > n.length && (f = s - ((o + 1) * s - n.length));
      const c = o * s,
        d = o * s + f,
        A = n.slice(c, d),
        y = new Uint8Array(2);
      ((y[0] = o & 255), (y[1] = (o & 65280) >> 8));
      const _ = new Uint8Array([
        this.FEA_CMD_SETTFTFLASHDATA,
        ...y,
        A.length,
        0,
        0,
        0,
        0,
        ...A,
      ]);
      if (!(await this.sendMsgByPath(_, h.Bit7, i, 5))) return !1;
      e(o / r);
    }
    return (e(1), !0);
  };
  getKeyMagnetismMode = async () => {};
  setKeyMagnetismMode = async (t) => !1;
  getFeatureList = async () => {};
  getPrecisionEnum = async () => {};
}
const j = (U) => {
  for (let t = 0; t <= U.length - 4; t++)
    if (U[t] === 0 && U[t + 1] === 0 && U[t + 2] === 0 && U[t + 3] === 0)
      return !0;
  return !1;
};
export { Q as C };
