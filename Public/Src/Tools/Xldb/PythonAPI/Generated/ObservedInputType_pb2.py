# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: ObservedInputType.proto

from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='ObservedInputType.proto',
  package='BuildXL.Xldb.Proto',
  syntax='proto3',
  serialized_options=b'\252\002\022BuildXL.Xldb.Proto',
  serialized_pb=b'\n\x17ObservedInputType.proto\x12\x12\x42uildXL.Xldb.Proto*\xad\x01\n\x11ObservedInputType\x12!\n\x1dObservedInputType_UNSPECIFIED\x10\x00\x12\x13\n\x0f\x41\x62sentPathProbe\x10\x01\x12\x13\n\x0f\x46ileContentRead\x10\x02\x12\x18\n\x14\x44irectoryEnumeration\x10\x03\x12\x1a\n\x16\x45xistingDirectoryProbe\x10\x04\x12\x15\n\x11\x45xistingFileProbe\x10\x05\x42\x15\xaa\x02\x12\x42uildXL.Xldb.Protob\x06proto3'
)

_OBSERVEDINPUTTYPE = _descriptor.EnumDescriptor(
  name='ObservedInputType',
  full_name='BuildXL.Xldb.Proto.ObservedInputType',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='ObservedInputType_UNSPECIFIED', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='AbsentPathProbe', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='FileContentRead', index=2, number=2,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='DirectoryEnumeration', index=3, number=3,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='ExistingDirectoryProbe', index=4, number=4,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='ExistingFileProbe', index=5, number=5,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=48,
  serialized_end=221,
)
_sym_db.RegisterEnumDescriptor(_OBSERVEDINPUTTYPE)

ObservedInputType = enum_type_wrapper.EnumTypeWrapper(_OBSERVEDINPUTTYPE)
ObservedInputType_UNSPECIFIED = 0
AbsentPathProbe = 1
FileContentRead = 2
DirectoryEnumeration = 3
ExistingDirectoryProbe = 4
ExistingFileProbe = 5


DESCRIPTOR.enum_types_by_name['ObservedInputType'] = _OBSERVEDINPUTTYPE
_sym_db.RegisterFileDescriptor(DESCRIPTOR)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)
