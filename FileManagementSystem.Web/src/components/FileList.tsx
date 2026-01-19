import { useMutation, useQueryClient } from '@tanstack/react-query';
import { fileApi } from '../services/api';
import type { FileItemDto } from '../types';

interface FileListProps {
  files: FileItemDto[];
  isLoading: boolean;
  totalCount: number;
}

const FileList = ({ files, isLoading, totalCount }: FileListProps) => {
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await fileApi.deleteFile(id, true); // Move to recycle bin
    },
    onSuccess: () => {
      // Invalidate and refetch files
      queryClient.invalidateQueries({ queryKey: ['files'] });
    },
  });

  const handleDelete = async (id: string, fileName: string) => {
    if (window.confirm(`Are you sure you want to delete "${fileName}"?`)) {
      try {
        await deleteMutation.mutateAsync(id);
      } catch (error) {
        console.error('Error deleting file:', error);
        alert(`Failed to delete ${fileName}`);
      }
    }
  };

  const formatSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  if (isLoading) {
    return <div>Loading files...</div>;
  }

  return (
    <div>
      <h2>Files ({totalCount})</h2>
      {files.length === 0 ? (
        <p>No files found</p>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #ddd' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #ccc', background: '#f5f5f5' }}>
              <th style={{ textAlign: 'left', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Name</th>
              <th style={{ textAlign: 'right', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Size</th>
              <th style={{ textAlign: 'left', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Type</th>
              <th style={{ textAlign: 'left', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Tags</th>
              <th style={{ textAlign: 'left', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Date</th>
              <th style={{ textAlign: 'center', padding: '0.75rem' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {files.map((file) => {
              // Extract just the filename from the path, removing drive letters and long paths
              const getFileName = (path: string) => {
                const normalized = path.replace(/^[A-Z]:\\?/, '').replace(/\\/g, '/');
                return normalized.split('/').pop() || path;
              };
              
              const fileName = getFileName(file.path);
              
              return (
                <tr key={file.id} style={{ borderBottom: '1px solid #ddd' }}>
                  <td style={{ padding: '0.75rem', borderRight: '1px solid #ddd' }}>{fileName}</td>
                  <td style={{ textAlign: 'right', padding: '0.75rem', borderRight: '1px solid #ddd' }}>{formatSize(file.size)}</td>
                  <td style={{ padding: '0.75rem', borderRight: '1px solid #ddd' }}>{file.mimeType}</td>
                  <td style={{ padding: '0.75rem', borderRight: '1px solid #ddd' }}>{file.tags.join(', ') || '-'}</td>
                  <td style={{ padding: '0.75rem', borderRight: '1px solid #ddd' }}>{new Date(file.createdDate).toLocaleDateString()}</td>
                  <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                    <button
                      onClick={() => handleDelete(file.id, fileName)}
                      disabled={deleteMutation.isPending}
                      style={{
                        padding: '0.25rem 0.75rem',
                        borderRadius: '4px',
                        border: '1px solid #dc3545',
                        background: '#dc3545',
                        color: '#fff',
                        cursor: deleteMutation.isPending ? 'not-allowed' : 'pointer',
                        fontSize: '0.875rem',
                        opacity: deleteMutation.isPending ? 0.6 : 1,
                      }}
                      title="Delete file"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default FileList;
