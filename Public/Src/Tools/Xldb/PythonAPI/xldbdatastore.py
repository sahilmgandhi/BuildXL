import Generated
from Generated import *
import rocksdb
import os

class XldbDataStore:
    event_column_family_name = b"Event"
    pip_column_family_name = b"Pip"
    static_graph_column_family_name = b"StaticGraph"
    path_table_family_name = b"PathTable"
    inverse_path_table_family_name = b"InversePathTable"
    string_table_family_name = b"StringTable"
    inverse_string_table_family_name = b"InverseStringTable"

    xldb_version_file_name = "xldbversion.txt"
    
    '''
    Constructor for the datastore
    @param path     The path to the rocksdb directory
    '''
    def __init__(self, path):
        column_families = {
            b'default': rocksdb.ColumnFamilyOptions(),
            pip_column_family_name : rocksdb.ColumnFamilyOptions(),
            event_column_family_name : rocksdb.ColumnFamilyOptions(),
            static_graph_column_family_name : rocksdb.ColumnFamilyOptions(),
            path_table_family_name : rocksdb.ColumnFamilyOptions(),
            string_table_family_name : rocksdb.ColumnFamilyOptions(),
            inverse_path_table_family_name : rocksdb.ColumnFamilyOptions(),
            inverse_string_table_family_name : rocksdb.ColumnFamilyOptions(),
        }

        class DynamicPrefix(rocksdb.interfaces.SliceTransform):
            def name(self):
                return b'dynamic'

            def transform(self, src):
                return (0, len(src))

            def in_domain(self, src):
                return True

            def in_range(self, dst):
                return True


        options = rocksdb.Options()
        options.prefix_extractor = DynamicPrefix()

        if not os.path.isdir(path):
            print("The path provided does not exist")
            sys.exit(1)

        self.db = rocksdb.DB(path, options, column_families=column_families)

    '''
    Gets the string from an id

    @param id       The id of the string
    '''
    def get_string_from_id(self, id):

        string_table_key = StringTableKey()
        string_table_key.id = id
        print()

        string_val = self.db.get(self.db.get_column_family(string_table_family_name), string_table_key)

        return FullString().ParseFromString(string_val).value