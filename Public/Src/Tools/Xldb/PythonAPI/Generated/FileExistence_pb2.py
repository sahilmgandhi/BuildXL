# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: FileExistence.proto

from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='FileExistence.proto',
  package='BuildXL.Xldb.Proto',
  syntax='proto3',
  serialized_options=b'\252\002\022BuildXL.Xldb.Proto',
  serialized_pb=b'\n\x13\x46ileExistence.proto\x12\x12\x42uildXL.Xldb.Proto*Y\n\rFileExistence\x12\x1d\n\x19\x46ileExistence_UNSPECIFIED\x10\x00\x12\x0c\n\x08Required\x10\x01\x12\r\n\tTemporary\x10\x02\x12\x0c\n\x08Optional\x10\x03\x42\x15\xaa\x02\x12\x42uildXL.Xldb.Protob\x06proto3'
)

_FILEEXISTENCE = _descriptor.EnumDescriptor(
  name='FileExistence',
  full_name='BuildXL.Xldb.Proto.FileExistence',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='FileExistence_UNSPECIFIED', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='Required', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='Temporary', index=2, number=2,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='Optional', index=3, number=3,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=43,
  serialized_end=132,
)
_sym_db.RegisterEnumDescriptor(_FILEEXISTENCE)

FileExistence = enum_type_wrapper.EnumTypeWrapper(_FILEEXISTENCE)
FileExistence_UNSPECIFIED = 0
Required = 1
Temporary = 2
Optional = 3


DESCRIPTOR.enum_types_by_name['FileExistence'] = _FILEEXISTENCE
_sym_db.RegisterFileDescriptor(DESCRIPTOR)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)