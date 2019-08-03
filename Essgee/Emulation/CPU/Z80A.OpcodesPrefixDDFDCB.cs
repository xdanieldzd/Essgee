﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essgee.Emulation.CPU
{
	public partial class Z80A
	{
		static DDFDCBOpcodeDelegate[] opcodesPrefixDDFDCB = new DDFDCBOpcodeDelegate[]
		{
			/* 0x00 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.RotateLeftCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.RotateRightCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.RotateRightCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.RotateRightCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.RotateRightCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.RotateRightCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.RotateRightCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.RotateRightCircular(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.RotateRightCircular(address); }),
			/* 0x10 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.RotateLeft(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.RotateRight(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.RotateRight(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.RotateRight(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.RotateRight(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.RotateRight(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.RotateRight(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.RotateRight(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.RotateRight(address); }),
			/* 0x20 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ShiftLeftArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ShiftRightArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ShiftRightArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ShiftRightArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ShiftRightArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ShiftRightArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ShiftRightArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ShiftRightArithmetic(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ShiftRightArithmetic(address); }),
			/* 0x30 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ShiftLeftLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ShiftRightLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ShiftRightLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ShiftRightLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ShiftRightLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ShiftRightLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ShiftRightLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ShiftRightLogical(address); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ShiftRightLogical(address); }),
			/* 0x40 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 1); }),
			/* 0x50 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 3); }),
			/* 0x60 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 5); }),
			/* 0x70 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.TestBit(address, 7); }),
			/* 0x80 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 1); }),
			/* 0x90 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 3); }),
			/* 0xA0 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 5); }),
			/* 0xB0 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.ResetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.ResetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.ResetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.ResetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.ResetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.ResetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.ResetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.ResetBit(address, 7); }),
			/* 0xC0 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 0); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 1); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 1); }),
			/* 0xD0 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 2); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 3); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 3); }),
			/* 0xE0 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 4); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 5); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 5); }),
			/* 0xF0 */
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 6); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.High = c.SetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.bc.Low = c.SetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.High = c.SetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.de.Low = c.SetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.High = c.SetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.hl.Low = c.SetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.SetBit(address, 7); }),
			new DDFDCBOpcodeDelegate((Z80A c, ref Register r, ushort address) => { c.af.High = c.SetBit(address, 7); }),
		};
	}
}
