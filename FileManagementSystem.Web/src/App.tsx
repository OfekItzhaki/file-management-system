import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import Dashboard from './components/Dashboard';
import './App.css';

import { ThemeProvider } from './context/ThemeContext';

const queryClient = new QueryClient(); // Simpler default options for now

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <BrowserRouter>
          <div className='app'>
            <Routes>
              <Route path='/' element={<Dashboard />} />
            </Routes>
            <Toaster
              position='bottom-right'
              toastOptions={{
                duration: 4000,
                style: {
                  background: 'var(--surface-primary)',
                  color: 'var(--text-primary)',
                  borderRadius: '8px',
                  border: '1px solid var(--border-color)',
                },
              }}
            />
          </div>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
