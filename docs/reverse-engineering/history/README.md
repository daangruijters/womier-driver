# Historical notes (April 2026)

These three documents come from an earlier analysis of the vendor application that aimed at reusing the
vendor's `iot_driver.exe` gRPC helper. They are kept because they document the helper's lifecycle, its protobuf
schema and the first pass over the packet builders. Two of their conclusions turned out to be wrong and are
corrected in `../README.md` and `../../protocol.md`:

* the keyboard can be reached directly over HID (the helper is not required);
* the model id comes from the `GET_USB_VERSION` answer, not from command `0xF1`.
