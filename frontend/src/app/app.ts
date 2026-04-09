import { Component, inject } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

// Import our custom UI components
import { Button } from './shared/components/button/button';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [Button],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private toastr = inject(ToastrService);

  // Demonstrate Toastr functionality
  showSuccessToast() {
    this.toastr.success('Your action was completed successfully!', 'Success');
  }

  showErrorToast() {
    this.toastr.error('Something went wrong during the operation.', 'Error');
  }

  showInfoToast() {
    this.toastr.info('A new update is available for SHIELDON.', 'Information');
  }

  // Demonstrate SweetAlert2 functionality
  showConfirmationDialog() {
    Swal.fire({
      title: 'Are you sure?',
      text: "You won't be able to revert this!",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#215DAE',
      cancelButtonColor: '#EF4444',
      confirmButtonText: 'Yes, delete it!',
      customClass: {
        popup: 'swal2-custom-popup',
        confirmButton: 'swal2-custom-confirm'
      }
    }).then((result) => {
      if (result.isConfirmed) {
        Swal.fire(
          'Deleted!',
          'Your file has been deleted.',
          'success'
        );
      }
    });
  }

  showInteractiveLoading() {
    Swal.fire({
      title: 'Processing Request',
      html: 'Please wait while we process your data...',
      timerProgressBar: true,
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });
    
    // Simulate an API call
    setTimeout(() => {
      Swal.close();
      this.toastr.success('Data processing complete!');
    }, 2000);
  }
}
