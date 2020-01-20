# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: PipExecutionStep.proto

from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='PipExecutionStep.proto',
  package='BuildXL.Xldb.Proto',
  syntax='proto3',
  serialized_options=b'\252\002\022BuildXL.Xldb.Proto',
  serialized_pb=b'\n\x16PipExecutionStep.proto\x12\x12\x42uildXL.Xldb.Proto*\xb3\x03\n\x10PipExecutionStep\x12 \n\x1cPipExecutionStep_UNSPECIFIED\x10\x00\x12\x19\n\x15PipExecutionStep_None\x10\x01\x12\t\n\x05Start\x10\x02\x12\n\n\x06\x43\x61ncel\x10\x03\x12\x1f\n\x1bSkipDueToFailedDependencies\x10\x04\x12\x18\n\x14\x43heckIncrementalSkip\x10\x05\x12\x15\n\x11MaterializeInputs\x10\x06\x12\x16\n\x12MaterializeOutputs\x10\x07\x12\x18\n\x14\x45xecuteNonProcessPip\x10\x08\x12 \n\x1cPipExecutionStep_CacheLookup\x10\t\x12\x10\n\x0cRunFromCache\x10\n\x12\x12\n\x0e\x45xecuteProcess\x10\x0b\x12\x0f\n\x0bPostProcess\x10\x0c\x12\x10\n\x0cHandleResult\x10\r\x12$\n PipExecutionStep_ChooseWorkerCpu\x10\x0e\x12,\n(PipExecutionStep_ChooseWorkerCacheLookup\x10\x0f\x12\x08\n\x04\x44one\x10\x10\x42\x15\xaa\x02\x12\x42uildXL.Xldb.Protob\x06proto3'
)

_PIPEXECUTIONSTEP = _descriptor.EnumDescriptor(
  name='PipExecutionStep',
  full_name='BuildXL.Xldb.Proto.PipExecutionStep',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='PipExecutionStep_UNSPECIFIED', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='PipExecutionStep_None', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='Start', index=2, number=2,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='Cancel', index=3, number=3,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='SkipDueToFailedDependencies', index=4, number=4,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='CheckIncrementalSkip', index=5, number=5,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='MaterializeInputs', index=6, number=6,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='MaterializeOutputs', index=7, number=7,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='ExecuteNonProcessPip', index=8, number=8,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='PipExecutionStep_CacheLookup', index=9, number=9,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='RunFromCache', index=10, number=10,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='ExecuteProcess', index=11, number=11,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='PostProcess', index=12, number=12,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='HandleResult', index=13, number=13,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='PipExecutionStep_ChooseWorkerCpu', index=14, number=14,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='PipExecutionStep_ChooseWorkerCacheLookup', index=15, number=15,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='Done', index=16, number=16,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=47,
  serialized_end=482,
)
_sym_db.RegisterEnumDescriptor(_PIPEXECUTIONSTEP)

PipExecutionStep = enum_type_wrapper.EnumTypeWrapper(_PIPEXECUTIONSTEP)
PipExecutionStep_UNSPECIFIED = 0
PipExecutionStep_None = 1
Start = 2
Cancel = 3
SkipDueToFailedDependencies = 4
CheckIncrementalSkip = 5
MaterializeInputs = 6
MaterializeOutputs = 7
ExecuteNonProcessPip = 8
PipExecutionStep_CacheLookup = 9
RunFromCache = 10
ExecuteProcess = 11
PostProcess = 12
HandleResult = 13
PipExecutionStep_ChooseWorkerCpu = 14
PipExecutionStep_ChooseWorkerCacheLookup = 15
Done = 16


DESCRIPTOR.enum_types_by_name['PipExecutionStep'] = _PIPEXECUTIONSTEP
_sym_db.RegisterFileDescriptor(DESCRIPTOR)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)
