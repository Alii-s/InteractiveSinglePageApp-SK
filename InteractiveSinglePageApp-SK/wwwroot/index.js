$(function () {
    $(document).scroll(function () {
      var $nav = $("#mainNavbar");
      $nav.toggleClass("scrolled", $(this).scrollTop() > $nav.height());
    });
  })
  
  
  const form = document.querySelector('form.login');
  const email = document.querySelector('#email');
  const password = document.querySelector('#password');
  const login = document.querySelector('#login');
  const card = document.querySelector('.card');
  const showPassword = document.querySelector('#showPassword');
  const faEye = document.querySelector('.fa-eye');
  const faEyeSlash = document.querySelector('.fa-eye-slash');
  const support=document.querySelector('#support')
  let successFlagP = false;
  let successFlagE = false;
  // validation
  const isValidEmail = email => {
    const re = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(String(email).toLowerCase());
  }
  
  form.addEventListener('submit', function (e) {
    e.preventDefault();
    e.stopImmediatePropagation();
    validateInputs();
  })
  
  const setError = (element, message) => {
    const inputControl = element.parentElement;
    const errorDisplay = inputControl.querySelector('.error');
  
    errorDisplay.innerText = message;
    inputControl.classList.add('error');
    inputControl.classList.remove('success');
  }
  
  const setSuccess = (element) => {
    const inputControl = element.parentElement;
    const errorDisplay = inputControl.querySelector('.error');
  
    errorDisplay.innerText = '';
    inputControl.classList.add('success');
    inputControl.classList.remove('error');
  }
  
  const validateInputs = () => {
    successFlagP = false;
    successFlagE = false;
    const emailValue = email.value.trim();
    const passwordValue = password.value.trim();
    if (emailValue === '') {
      setError(email, 'Email is required.');
    } else if (!isValidEmail(emailValue)) {
      setError(email, 'Please enter a valid email.');
    } else {
      setSuccess(email);
      successFlagE = true;
    }
  
    if (passwordValue === '') {
      setError(password, 'Password is required.');
      faEye.style.bottom = '40%'
    } else if (passwordValue.length < 8) {
      setError(password, 'Password must be at least 8 characters.');
      faEye.style.bottom = '40%'
    } else {
      setSuccess(password);
      successFlagP = true;
    }
    if (successFlagP && successFlagE) {
      window.location.href = "../TEA-BANK.HTML"
  
    }
    // validation end
  
  }
    showPassword.addEventListener('click', function (e) {
      this.classList.toggle("fa-eye");
      this.classList.toggle('fa-eye-slash');
      if (password.getAttribute('type') === 'password') {
        password.setAttribute('type', 'text');
      }
      else {
        password.setAttribute('type', 'password');
      }
    })
    if (!(localStorage.getItem('isLoggedIn') === 'true')) {  
      support.classList.toggle('d-none')
    }
  